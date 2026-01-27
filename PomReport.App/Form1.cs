using System;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using System.Threading;

using System.Threading.Tasks;

using System.Windows.Forms;

using PomReport.Config;

using PomReport.Config.Models;

using PomReport.Data.Sql;

namespace PomReport.App

{

    public partial class Form1 : Form

    {

        private readonly BindingSource _airplanesBinding = new BindingSource();

        public Form1()

        {

            InitializeComponent();

            Load += Form1_Load;

            // Wire events (designer does NOT wire these)

            btnPull.Click += btnPull_Click;

            btnAdd.Click += btnAdd_Click;

            btnRemoveSelected.Click += btnRemoveSelected_Click;

            // Grid setup

            dataGridAirplanes.AutoGenerateColumns = false;

            dataGridAirplanes.MultiSelect = true;

            dataGridAirplanes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            _airplanesBinding.DataSource = new List<AirplanePair>();

            dataGridAirplanes.DataSource = _airplanesBinding;

        }

        private ShopConfig LoadConfig()

        {

            ConfigStore.CreateDefaultIfMissing();

            return ConfigStore.Load();

        }

        private void SaveConfig(ShopConfig cfg)

        {

            cfg.LastUpdatedUtc = DateTime.UtcNow;

            ConfigStore.Save(cfg);

        }

        private void Form1_Load(object? sender, EventArgs e)

        {

            try

            {

                var cfg = LoadConfig();

                Log($"Config loaded from: {ConfigStore.ConfigPath}");

                Log($"Airplanes in config: {cfg.Airplanes.Count}");

                // bind airplanes

                _airplanesBinding.DataSource = cfg.Airplanes;

                _airplanesBinding.ResetBindings(false);

                UpdateSqlPreview(cfg);

            }

            catch (Exception ex)

            {

                MessageBox.Show(ex.Message, "Config load failed");

            }

        }

        private void UpdateSqlPreview(ShopConfig cfg)

        {

            var values = cfg.Airplanes

                .SelectMany(a => new[] { a.Vh, a.Vz })

                .Where(s => !string.IsNullOrWhiteSpace(s))

                .Select(s => $"'{s.Trim()}'");

            var list = string.Join(", ", values);

            textQueryPreview.Text =

                "-- SQL Preview\n" +

                $"AND jl.LineNumber IN ({list})";

        }

        private void btnAdd_Click(object? sender, EventArgs e)

        {

            try

            {

                var cfg = LoadConfig();

                var vh = (txtVh.Text ?? "").Trim();

                var vz = (txtVz.Text ?? "").Trim();

                var loc = (txtLocation.Text ?? "").Trim();

                // Rules:

                // - You said: boom shop allows VZ without VH -> allow blank VH as long as VZ exists

                // - But never allow both blank

                if (string.IsNullOrWhiteSpace(vh) && string.IsNullOrWhiteSpace(vz))

                {

                    MessageBox.Show("Enter at least VH or VZ.", "Add airplane");

                    return;

                }

                // Optional: avoid exact duplicates (same VH+VZ+Location)

                bool duplicate = cfg.Airplanes.Any(a =>

                    string.Equals((a.Vh ?? "").Trim(), vh, StringComparison.OrdinalIgnoreCase) &&

                    string.Equals((a.Vz ?? "").Trim(), vz, StringComparison.OrdinalIgnoreCase) &&

                    string.Equals((a.Location ?? "").Trim(), loc, StringComparison.OrdinalIgnoreCase)

                );

                if (duplicate)

                {

                    MessageBox.Show("That row already exists.", "Add airplane");

                    return;

                }

                cfg.Airplanes.Add(new AirplanePair

                {

                    Vh = vh,

                    Vz = vz,

                    Location = loc

                });

                SaveConfig(cfg);

                // refresh binding

                _airplanesBinding.DataSource = cfg.Airplanes;

                _airplanesBinding.ResetBindings(false);

                // clear inputs

                txtVh.Text = "";

                txtVz.Text = "";

                txtLocation.Text = "";

                UpdateSqlPreview(cfg);

            }

            catch (Exception ex)

            {

                Log(ex.ToString());

                MessageBox.Show(ex.Message, "Add failed");

            }

        }

        private void btnRemoveSelected_Click(object? sender, EventArgs e)

        {

            try

            {

                if (dataGridAirplanes.SelectedRows.Count == 0)

                {

                    MessageBox.Show("Select one or more rows to remove.", "Remove");

                    return;

                }

                var cfg = LoadConfig();

                // Gather selected items

                var selected = new List<AirplanePair>();

                foreach (DataGridViewRow row in dataGridAirplanes.SelectedRows)

                {

                    if (row.DataBoundItem is AirplanePair ap)

                        selected.Add(ap);

                }

                if (selected.Count == 0) return;

                // TRUE DELETE

                foreach (var ap in selected)

                    cfg.Airplanes.Remove(ap);

                SaveConfig(cfg);

                _airplanesBinding.DataSource = cfg.Airplanes;

                _airplanesBinding.ResetBindings(false);

                UpdateSqlPreview(cfg);

            }

            catch (Exception ex)

            {

                Log(ex.ToString());

                MessageBox.Show(ex.Message, "Remove failed");

            }

        }

        private async void btnPull_Click(object? sender, EventArgs e)

        {

            try

            {

                var cfg = LoadConfig();

                // Build VH/VZ list for LineNumber IN (...)

                var lineNumbers = cfg.Airplanes

                    .SelectMany(a => new[] { a.Vh, a.Vz })

                    .Where(s => !string.IsNullOrWhiteSpace(s))

                    .Select(s => s.Trim())

                    .Distinct(StringComparer.OrdinalIgnoreCase)

                    .ToList();

                if (lineNumbers.Count == 0)

                {

                    MessageBox.Show("No VH/VZ values found.", "Nothing to run");

                    return;

                }

                var outputDir = Path.Combine(

                    AppContext.BaseDirectory,

                    cfg.ExportFolderName ?? "exports"

                );

                Directory.CreateDirectory(outputDir);

                Log("Running query...");

                Log($"Output directory: {outputDir}");

                var cts = new CancellationTokenSource();

                var outFile = await SqlJobSource.PullToCsvAsync(

                    cfg.ConnectionString ?? "",

                    SqlQueries.MainQuery,

                    lineNumbers,

                    outputDir,

                    "PomReport",

                    50000,

                    cts.Token

                );

                Log($"CSV written: {outFile}");

            }

            catch (Exception ex)

            {

                Log(ex.ToString());

                MessageBox.Show(ex.Message, "Run failed");

            }

        }

        private void Log(string message)

        {

            _log.AppendText(message + Environment.NewLine);

        }

    }

}
 
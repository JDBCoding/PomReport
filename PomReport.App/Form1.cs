using System;

using System.ComponentModel;

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

        private readonly BindingList<AirplanePair> _pairs = new();

        public Form1()

        {

            InitializeComponent();

            Load += Form1_Load;

            btnPull.Click += btnPull_Click;

            btnAdd.Click += btnAdd_Click;

            btnRemoveSelected.Click += btnRemoveSelected_Click;

        }

        private ShopConfig LoadConfig()

        {

            ConfigStore.CreateDefaultIfMissing();

            return ConfigStore.Load();

        }

        private void SaveConfig()

        {

            var cfg = LoadConfig();

            cfg.Airplanes = _pairs.ToList();

            cfg.LastUpdatedUtc = DateTime.UtcNow;

            ConfigStore.Save(cfg);

            Log($"Saved config: {ConfigStore.ConfigPath}");

        }

        private void Form1_Load(object? sender, EventArgs e)

        {

            try

            {

                var cfg = LoadConfig();

                _pairs.Clear();

                foreach (var p in cfg.Airplanes)

                    _pairs.Add(p);

                dataGridAirplanes.DataSource = _pairs;

                Log($"Config loaded from: {ConfigStore.ConfigPath}");

                Log($"Airplanes in config: {cfg.Airplanes.Count}");

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

            var vh = txtVh.Text?.Trim() ?? "";

            var vz = txtVz.Text?.Trim() ?? "";

            var loc = txtLocation.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(vh) && string.IsNullOrWhiteSpace(vz))

            {

                MessageBox.Show("Enter at least VH or VZ.", "Missing value");

                return;

            }

            _pairs.Add(new AirplanePair { Vh = vh, Vz = vz, Location = loc });

            txtVh.Clear();

            txtVz.Clear();

            txtLocation.Clear();

            SaveConfig();

            UpdateSqlPreview(LoadConfig());

        }

        private void btnRemoveSelected_Click(object? sender, EventArgs e)

        {

            if (dataGridAirplanes.SelectedRows.Count == 0)

                return;

            var toRemove = dataGridAirplanes.SelectedRows

                .Cast<DataGridViewRow>()

                .Select(r => r.DataBoundItem)

                .OfType<AirplanePair>()

                .ToList();

            foreach (var p in toRemove)

                _pairs.Remove(p);

            SaveConfig();

            UpdateSqlPreview(LoadConfig());

        }

        private async void btnPull_Click(object? sender, EventArgs e)

        {

            try

            {

                var cfg = LoadConfig();

                if (string.IsNullOrWhiteSpace(cfg.ConnectionString))

                {

                    MessageBox.Show("config.json is missing 'connectionString'.", "Missing config");

                    return;

                }

                var lineNumbers = cfg.Airplanes

                    .SelectMany(a => new[] { a.Vh, a.Vz })

                    .Where(s => !string.IsNullOrWhiteSpace(s))

                    .Select(s => s.Trim())

                    .Distinct()

                    .ToList();

                if (lineNumbers.Count == 0)

                {

                    MessageBox.Show("No VH/VZ values found.", "Nothing to run");

                    return;

                }

                var outputDir = Path.Combine(AppContext.BaseDirectory, cfg.ExportFolderName ?? "exports");

                Directory.CreateDirectory(outputDir);

                Log("Running query...");

                Log($"Output directory: {outputDir}");

                using var cts = new CancellationTokenSource();

                var outFile = await SqlJobSource.PullToCsvAsync(

                    cfg.ConnectionString,

                    SqlQueries.MainQuery,

                    lineNumbers,

                    outputDir,

                    "PomReport",

                    commandTimeoutSeconds: 120,

                    cts.Token

                );

                Log($"CSV written: {outFile}");

                MessageBox.Show($"CSV written:\n{outFile}", "Success");

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
 
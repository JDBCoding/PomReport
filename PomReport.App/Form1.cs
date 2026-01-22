using System;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using System.Reflection;

using System.Threading.Tasks;

using System.Windows.Forms;

using PomReport.Config;

using PomReport.Data.Sql;

namespace PomReport.App

{

    public partial class Form1 : Form

    {

        public Form1()

        {

            InitializeComponent();

        }

        // IMPORTANT: Designer is wired to this name.

        private async void btnPull_Click(object sender, EventArgs e)

        {

            // Disable button so user can’t double-click and start 2 pulls

            btnPull.Enabled = false;

            try

            {

                // 1) Ensure config exists

                if (!ConfigStore.Exists())

                {

                    using var setup = new SetupForm();

                    if (setup.ShowDialog(this) != DialogResult.OK)

                        return;

                }

                // 2) Load config

                var cfg = ConfigStore.Load();

                // 3) Log current config contents (same as before)

                _log.Clear();

                _log.AppendText($"Shop: {cfg.ShopName}{Environment.NewLine}");

                _log.AppendText($"Config: {ConfigStore.ConfigPath}{Environment.NewLine}{Environment.NewLine}");

                _log.AppendText("VH/VZ pairs:" + Environment.NewLine);

                foreach (var a in cfg.Airplanes)

                {

                    var loc = string.IsNullOrWhiteSpace(a.Location) ? "" : $" | {a.Location}";

                    _log.AppendText($"  {a.Vh} = {a.Vz}{loc}{Environment.NewLine}");

                }

                _log.AppendText(Environment.NewLine + "Job Categories:" + Environment.NewLine);

                foreach (var j in cfg.JobCategories)

                {

                    _log.AppendText($"  {j.Ip} = {j.Category}{Environment.NewLine}");

                }

                // Optional: show preview area if it exists

                if (textQueryPreview != null)

                {

                    var vhList = cfg.Airplanes.Select(x => x.Vh).ToList();

                    var vzList = cfg.Airplanes.Select(x => x.Vz).ToList();

                    textQueryPreview.Text =

                        "-- Inputs loaded from config\r\n" +

                        $"-- VH count: {vhList.Count}, VZ count: {vzList.Count}\r\n" +

                        $"-- Example VH: {string.Join(", ", vhList.Take(5))}\r\n" +

                        $"-- Example VZ: {string.Join(", ", vzList.Take(5))}\r\n";

                }

                // 4) Build the line-number list

                // Default behavior: use all VH + all VZ as line numbers (distinct)

                var lineNumbers = new List<string>();

                lineNumbers.AddRange(cfg.Airplanes.Select(a => a.Vh));

                lineNumbers.AddRange(cfg.Airplanes.Select(a => a.Vz));

                lineNumbers = lineNumbers

                    .Select(x => x?.Trim())

                    .Where(x => !string.IsNullOrWhiteSpace(x))

                    .Distinct(StringComparer.OrdinalIgnoreCase)

                    .ToList();

                // If you have a textbox called textLineNumbers and user typed something,

                // we will USE THAT INSTEAD (so you can override quickly without editing config)

                if (textLineNumbers != null && !string.IsNullOrWhiteSpace(textLineNumbers.Text))

                {

                    var typed = ParseLineNumbers(textLineNumbers.Text);

                    if (typed.Count > 0)

                        lineNumbers = typed;

                }

                if (lineNumbers.Count == 0)

                {

                    MessageBox.Show("No line numbers found. Add VH/VZ pairs in Setup first.", "PomReport",

                        MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;

                }

                // 5) Get SQL connection string from config (reflection so it won’t break if property name differs)

                var connString = TryGetStringProperty(cfg, "SqlConnectionString")

                              ?? TryGetStringProperty(cfg, "ConnectionString")

                              ?? TryGetStringProperty(cfg, "SqlConnString");

                if (string.IsNullOrWhiteSpace(connString))

                {

                    MessageBox.Show(

                        "SQL Connection String is not saved in your config yet.\n\n" +

                        "Open Setup and save the connection string, then run again.",

                        "PomReport",

                        MessageBoxButtons.OK,

                        MessageBoxIcon.Warning);

                    return;

                }

                // 6) Decide output folder (default: same folder as config file)

                var outputFolder = TryGetStringProperty(cfg, "OutputFolder")

                                ?? TryGetStringProperty(cfg, "ExportFolder")

                                ?? TryGetStringProperty(cfg, "DataFolder");

                if (string.IsNullOrWhiteSpace(outputFolder))

                {

                    outputFolder = Path.GetDirectoryName(ConfigStore.ConfigPath) ?? Environment.CurrentDirectory;

                }

                Directory.CreateDirectory(outputFolder);

                _log.AppendText(Environment.NewLine);

                _log.AppendText($"Pulling SQL for {lineNumbers.Count} line numbers...{Environment.NewLine}");

                _log.AppendText($"Output folder: {outputFolder}{Environment.NewLine}");

                // 7) Run SQL pull -> CSV (ASYNC)

                var csvPath = await SqlJobSource.PullToCsvAsync(

                    connString,

                    SqlQueries.MainQuery,

                    lineNumbers,

                    outputFolder,

                    filePrefix: "PomReport_SQL_Pull");

                _log.AppendText($"DONE ✅ Saved CSV:{Environment.NewLine}{csvPath}{Environment.NewLine}");

                MessageBox.Show($"Saved CSV:\n{csvPath}", "PomReport", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }

            catch (Exception ex)

            {

                MessageBox.Show(ex.ToString(), "PomReport Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

            finally

            {

                btnPull.Enabled = true;

            }

        }

        private static List<string> ParseLineNumbers(string input)

        {

            // Accept comma, space, newline, tab separators

            var parts = input

                .Split(new[] { ',', '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)

                .Select(x => x.Trim())

                .Where(x => !string.IsNullOrWhiteSpace(x))

                .Distinct(StringComparer.OrdinalIgnoreCase)

                .ToList();

            return parts;

        }

        private static string? TryGetStringProperty(object obj, string propertyName)

        {

            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (prop == null) return null;

            if (prop.PropertyType != typeof(string)) return null;

            return prop.GetValue(obj) as string;

        }

    }

}
 
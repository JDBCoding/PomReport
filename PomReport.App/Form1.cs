using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using PomReport.Config;
using PomReport.Config.Models;
using PomReport.App.Reporting;
using PomReport.Core.Services;
using PomReport.Data.Csv;
using PomReport.Data.Sql;
namespace PomReport.App {
    public partial class Form1 : Form {
        private BindingList<AirplanePair> _pairs = new();
        private FlowLayoutPanel? _rootPanel;
        private DataGridView? _grid;
        private Button? _btnSetup;
        private Button? _btnPull;
        private Button? _btnReport;
        public Form1() {
            InitializeComponent();
            Text = "POM Report Generator";
            BuildMainUi();
            Load += (_, __) => LoadConfigIntoGrid();
        }
        private void BuildMainUi() {
            _rootPanel = new FlowLayoutPanel {
                Dock = DockStyle.Top,
                Height = 260,
                AutoSize = false,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(12, 12, 12, 8)
            };
            var btnRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            _btnSetup = new Button { Text = "Setup…", Width = 110, Height = 28 };
            _btnSetup.Click += (_, __) => {
                using var setup = new SetupForm();
                if (setup.ShowDialog(this) == DialogResult.OK)
                    LoadConfigIntoGrid();
            };
            btnRow.Controls.Add(_btnSetup);
            _btnPull = new Button { Text = "Pull Data", Width = 110, Height = 28, Margin = new Padding(8, 2, 0, 0) };
            _btnPull.Click += btnPull_Click;
            btnRow.Controls.Add(_btnPull);
            _btnReport = new Button { Text = "Generate Report", Width = 140, Height = 28, Margin = new Padding(8, 2, 0, 0) };
            _btnReport.Click += btnTestPipeline_Click;   // uses the existing preview pipeline
            btnRow.Controls.Add(_btnReport);
            _grid = new DataGridView {
                Width = 760,
                Height = 200,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AirplanePair.LineNumber), HeaderText = "Line Number", Width = 110 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AirplanePair.Vh), HeaderText = "VH", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AirplanePair.Vz), HeaderText = "VZ", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AirplanePair.Location), HeaderText = "Location", Width = 140 });
            _grid.DataSource = _pairs;
            _rootPanel.Controls.Add(btnRow);
            _rootPanel.Controls.Add(_grid);
            Controls.Add(_rootPanel);
            _rootPanel.BringToFront();
        }
        private void LoadConfigIntoGrid() {
            ConfigStore.CreateDefaultIfMissing();
            var cfg = ConfigStore.Load();
            _pairs = new BindingList<AirplanePair>(cfg.Airplanes ?? new List<AirplanePair>());
            if (_grid != null) _grid.DataSource = _pairs;
        }
        // IMPORTANT: LineNumber is never used for SQL. VH/VZ only.
        private static List<string> BuildSqlIdentifiers(List<AirplanePair> airplanes) {
            var list = new List<string>();
            foreach (var a in airplanes) {
                if (!string.IsNullOrWhiteSpace(a.Vh)) list.Add(a.Vh.Trim());
                if (!string.IsNullOrWhiteSpace(a.Vz)) list.Add(a.Vz.Trim());
            }
            // Distinct (case-insensitive) preserving order
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var distinct = new List<string>();
            foreach (var s in list)
                if (seen.Add(s)) distinct.Add(s);
            return distinct;
        }
        private static string ReadSqlTemplate() {
            var path = Path.Combine(AppContext.BaseDirectory, "Copy_SQL_query.txt");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing SQL template file next to EXE: {path}");
            var text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Copy_SQL_query.txt is empty.");
            if (!text.Contains("{LINE_NUMBER_PARAMS}", StringComparison.Ordinal))
                throw new InvalidOperationException("Copy_SQL_query.txt must contain {LINE_NUMBER_PARAMS} in the IN(...) clause.");
            return text;
        }
        private static void ApplyFourHourRotation(string exportDir, TimeSpan keepOldFor) {
            var oldCsv = Path.Combine(exportDir, "olddata.csv");
            var oldJson = Path.Combine(exportDir, "olddata.airplanes.json");
            var newCsv = Path.Combine(exportDir, "newdata.csv");
            var newJson = Path.Combine(exportDir, "newdata.airplanes.json");
            if (!File.Exists(oldCsv))
                return; // no old yet -> nothing to rotate
            var oldAge = DateTime.UtcNow - File.GetLastWriteTimeUtc(oldCsv);
            // Only rotate/promote when old is older than keepOldFor
            if (oldAge <= keepOldFor)
                return;
            // Promote current new -> old (only if new exists)
            if (File.Exists(newCsv)) {
                File.Copy(newCsv, oldCsv, overwrite: true);
                if (File.Exists(newJson))
                    File.Copy(newJson, oldJson, overwrite: true);
                // Stamp old as "fresh" at the time of promotion (so it holds for 4 hours again)
                File.SetLastWriteTimeUtc(oldCsv, DateTime.UtcNow);
                if (File.Exists(oldJson))
                    File.SetLastWriteTimeUtc(oldJson, DateTime.UtcNow);
            }
        }

        // These exist ONLY because the WinForms Designer is wired to them.
        // Keep them as thin wrappers so Designer never breaks again.
        private void Btnpull_click(object sender, EventArgs e) {
            btnPull_Click(sender, e);
        }
        private void btnTestPipeline_Click(object sender, EventArgs e) {
            try {
                var cfg = ConfigStore.Load();
                var exportDir = Path.Combine(AppContext.BaseDirectory, cfg.ExportFolderName ?? "exports");
                Directory.CreateDirectory(exportDir);

                var previewPath = ReportPreviewService.GenerateHtmlPreview(
                    exportDir: exportDir,
                    shopName: cfg.ShopName ?? "",
                    airplanePairs: _pairs.ToList());

                ReportPreviewService.OpenInDefaultBrowser(previewPath);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Report Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void btnPull_Click(object? sender, EventArgs e) {
            try {
                var cfg = ConfigStore.Load();
                if (string.IsNullOrWhiteSpace(cfg.ConnectionString))
                    throw new InvalidOperationException("Missing ConnectionString in config.json.");
                var airplanes = _pairs.ToList();
                var identifiers = BuildSqlIdentifiers(airplanes);
                if (identifiers.Count == 0)
                    throw new InvalidOperationException("No VH/VZ values configured. Click Setup… and add at least one row with VH or VZ.");
                var sqlTemplate = ReadSqlTemplate();
                var exportDir = Path.Combine(AppContext.BaseDirectory, cfg.ExportFolderName ?? "exports");
                Directory.CreateDirectory(exportDir);
                // 4-hour rule
                ApplyFourHourRotation(exportDir, TimeSpan.FromHours(4));
                var newCsv = Path.Combine(exportDir, "newdata.csv");
                var newJson = Path.Combine(exportDir, "newdata.airplanes.json");
                // Write sidecar for traceability (exact airplane rows used in this pull)
                var sidecarPayload = new {
                    pulledUtc = DateTime.UtcNow,
                    shopName = cfg.ShopName ?? "",
                    airplanes = airplanes.Select(a => new {
                        lineNumber = a.LineNumber ?? "",
                        vh = a.Vh ?? "",
                        vz = a.Vz ?? "",
                        location = a.Location ?? ""
                    }).ToList()
                };
                File.WriteAllText(newJson, JsonSerializer.Serialize(sidecarPayload, new JsonSerializerOptions { WriteIndented = true }));
                // Execute SQL -> overwrite newdata.csv
                using var cts = new CancellationTokenSource();
                await SqlJobSource.PullToCsvAsync(
                    cfg.ConnectionString,
                    sqlTemplate,
                    identifiers,
                    newCsv,
                    commandTimeoutSeconds: 300,
                    ct: cts.Token);

                // NCR one-off categorization (human-in-the-loop, ask once and remember)
                TryCategorizeNewNcrJobs(newCsv);

                MessageBox.Show($"Pull complete.\n\n{newCsv}", "PomReport", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Pull Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TryCategorizeNewNcrJobs(string newCsvPath) {
            try {
                // Load jobs from the newly pulled CSV
                var jobs = CsvJobSource.Load(newCsvPath);

                // Identify NCR rows and extract NCR ids
                var ncrRows = jobs
                    .Select(j => new { Job = j, NcrId = NcrIdParser.TryExtract(j.JobKitDescription) })
                    .Where(x => x.NcrId != null)
                    .ToList();

                if (ncrRows.Count == 0)
                    return;

                var repo = new NcrOverrideRepository(NcrOverrideRepository.DefaultPath());
                var map = repo.Load();

                // Find NCR ids we have not categorized yet
                var missing = ncrRows
                    .Select(x => x.NcrId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(id => !map.ContainsKey(id))
                    .ToList();

                if (missing.Count == 0)
                    return; // nothing new

                // Build UI rows for missing NCRs (show a useful summary per NCR id)
                var items = missing.Select(id => {
                    var sample = ncrRows.First(x => id.Equals(x.NcrId, StringComparison.OrdinalIgnoreCase)).Job;
                    return new NcrCategorizationItem(
                        NcrId: id,
                        LineNumber: sample.LineNumber,
                        WorkOrder: sample.WorkOrder,
                        Summary: BuildNcrSummary(sample));
                }).ToList();

                using var dlg = new NcrCategorizationForm(items);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                foreach (var kvp in dlg.Results) {
                    // enforce allowed categories
                    if (!NcrCategorizationForm.AllowedCategories.Contains(kvp.Value, StringComparer.OrdinalIgnoreCase))
                        continue;

                    map[kvp.Key] = kvp.Value;
                }

                repo.Save(map);
            }
            catch {
                // This feature should never block the pull.
            }
        }

        private static string BuildNcrSummary(PomReport.Core.Core.Models.JobRecord j) {
            // Keep this short; user can open the CSV for full detail.
            var desc = (j.JobKitDescription ?? "").Trim();
            var notes = (j.JobNotes ?? "").Trim();
            var comments = (j.JobComments ?? "").Trim();

            string Take(string s, int max) {
                if (string.IsNullOrWhiteSpace(s)) return "";
                s = s.Replace("\r", " ").Replace("\n", " ").Trim();
                return s.Length <= max ? s : s.Substring(0, max) + "…";
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(desc)) parts.Add(Take(desc, 90));
            if (!string.IsNullOrWhiteSpace(notes) && notes != "-") parts.Add("Notes: " + Take(notes, 80));
            if (!string.IsNullOrWhiteSpace(comments) && comments != "-") parts.Add("Comment: " + Take(comments, 80));
            return string.Join(" | ", parts);
        }
    }
}
using System;

using System.Collections.Generic;

using System.ComponentModel;

using System.IO;

using System.Linq;

using System.Threading;

using System.Threading.Tasks;

using System.Text.Json;

using System.Windows.Forms;

using PomReport.Config;

using PomReport.Config.Models;

using PomReport.Data.Sql;

using PomReport.Core.Models;

using PomReport.Core.Services;



namespace PomReport.App {

    public partial class Form1 : Form {

        private readonly BindingList<AirplanePair> _pairs = new();

        private FlowLayoutPanel? _rootPanel;

        private DataGridView? _grid;

        private TextBox? _txtVh;

        private TextBox? _txtVz;

        private TextBox? _txtLocation;

        private Button? _btnAdd;

        private Button? _btnRemove;

        private Button? _btnPull;

        private Button? _btnTestPipeline;

        public Form1() {

            InitializeComponent();

            Text = "Form1 - Testing";

            BuildAirplaneEditorUi();

            // Load config into grid + preview on startup

            Load += (_, __) => LoadConfigIntoGridAndPreview();

        }

        // ------------------------------------------------------------

        // UI BUILD (Table + Add/Remove only)

        // ------------------------------------------------------------

        private void BuildAirplaneEditorUi() {

            _rootPanel = new FlowLayoutPanel {

                Dock = DockStyle.Top,

                Height = 220,

                AutoSize = false,

                WrapContents = false,

                FlowDirection = FlowDirection.TopDown,

                Padding = new Padding(12, 12, 12, 8)

            };

            var addRow = new FlowLayoutPanel {

                AutoSize = true,

                WrapContents = false

            };

            addRow.Controls.Add(new Label { Text = "VH:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });

            _txtVh = new TextBox { Width = 90, PlaceholderText = "VH123" };

            addRow.Controls.Add(_txtVh);

            addRow.Controls.Add(new Label { Text = "VZ:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });

            _txtVz = new TextBox { Width = 90, PlaceholderText = "VZ901" };

            addRow.Controls.Add(_txtVz);

            addRow.Controls.Add(new Label { Text = "Location:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });

            _txtLocation = new TextBox { Width = 140, PlaceholderText = "Stall 212" };

            addRow.Controls.Add(_txtLocation);

            _btnAdd = new Button { Text = "Add", Width = 80, Height = 28, Margin = new Padding(12, 2, 0, 0) };

            _btnAdd.Click += (_, __) => AddPair();

            addRow.Controls.Add(_btnAdd);

            _btnRemove = new Button { Text = "Remove Selected", Width = 140, Height = 28, Margin = new Padding(8, 2, 0, 0) };

            _btnRemove.Click += (_, __) => RemoveSelectedPair();

            addRow.Controls.Add(_btnRemove);

            // New buttons (Pull + Test Pipeline)

            _btnPull = new Button { Text = "Pull Data", Width = 110, Height = 28, Margin = new Padding(12, 2, 0, 0) };

            _btnPull.Click += Btnpull_click;

            addRow.Controls.Add(_btnPull);

            _btnTestPipeline = new Button { Text = "Test Pipeline", Width = 120, Height = 28, Margin = new Padding(8, 2, 0, 0) };

            _btnTestPipeline.Click += btnTestPipeline_Click;

            addRow.Controls.Add(_btnTestPipeline);

            _grid = new DataGridView {

                Width = 760,

                Height = 160,

                AutoGenerateColumns = false,

                AllowUserToAddRows = false,

                AllowUserToDeleteRows = false,

                AllowUserToResizeRows = false,

                ReadOnly = true, // add/remove ONLY

                SelectionMode = DataGridViewSelectionMode.FullRowSelect,

                MultiSelect = false

            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn {

                DataPropertyName = nameof(AirplanePair.Vh),

                HeaderText = "VH",

                Width = 90

            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn {

                DataPropertyName = nameof(AirplanePair.Vz),

                HeaderText = "VZ",

                Width = 90

            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn {

                DataPropertyName = nameof(AirplanePair.Location),

                HeaderText = "Location",

                Width = 140

            });

           _grid.DataSource = _pairs;

            _rootPanel.Controls.Add(addRow);

            _rootPanel.Controls.Add(_grid);

            Controls.Add(_rootPanel);

            _rootPanel.BringToFront();

        }

        // ------------------------------------------------------------

        // CONFIG LOAD/SAVE + PREVIEW

        // ------------------------------------------------------------

        private void LoadConfigIntoGridAndPreview() {

            try {

                if (!ConfigStore.Exists()) {

                    _log.AppendText("No config.json found. Use SetupForm on first run." + Environment.NewLine);

                    return;

                }

                var cfg = ConfigStore.Load();

                _pairs.Clear();

                foreach (var p in cfg.Airplanes)

                    _pairs.Add(p);

                var lineNumbers = BuildLineNumbersForSql(cfg.Airplanes);

                _log.Clear();

                _log.AppendText($"Config: {ConfigStore.ConfigPath}{Environment.NewLine}");

                _log.AppendText($"Airplanes rows: {_pairs.Count}{Environment.NewLine}");

                _log.AppendText($"LineNumbers passed to SQL: {lineNumbers.Count}{Environment.NewLine}");

                // Preview clause

                var clause = "AND jl.LineNumber IN (" +

                             string.Join(", ", lineNumbers.Select(x => $"'{x}'")) +

                             ")";

            }

            catch (Exception ex) {

                _log.AppendText(ex.ToString() + Environment.NewLine);

            }

        }

        private static List<string> BuildLineNumbersForSql(List<AirplanePair> airplanes) {

            var list = new List<string>();

            foreach (var a in airplanes) {

                if (!string.IsNullOrWhiteSpace(a.Vh)) list.Add(a.Vh.Trim());

                if (!string.IsNullOrWhiteSpace(a.Vz)) list.Add(a.Vz.Trim());

            }

            // Make distinct while preserving order (case-insensitive)

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var distinct = new List<string>();

            foreach (var s in list) {

                if (seen.Add(s))

                    distinct.Add(s);

            }

            return distinct;

        }

        private void AddPair() {

            if (_txtVh == null || _txtVz == null || _txtLocation == null) return;

            var vh = (_txtVh.Text ?? "").Trim();

            var vz = (_txtVz.Text ?? "").Trim();

            var loc = (_txtLocation.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(vh) && string.IsNullOrWhiteSpace(vz))

                return;

            _pairs.Add(new AirplanePair { Vh = vh, Vz = vz, Location = loc });

            _txtVh.Text = "";

            _txtVz.Text = "";

            _txtLocation.Text = "";

        }

        private void RemoveSelectedPair() {

            if (_grid == null) return;

            if (_grid.SelectedRows.Count == 0) return;

            var row = _grid.SelectedRows[0];

            if (row.DataBoundItem is AirplanePair pair)

                _pairs.Remove(pair);

        }

        // ------------------------------------------------------------

        // BUTTON: Pull DB -> Save CSV (alias for designer request)

        // ------------------------------------------------------------

        private void Btnpull_click(object sender, EventArgs e) {

            // Wrapper to keep existing handler intact

            btnPull_Click(sender, e);

        }

        // ------------------------------------------------------------

        // BUTTON: Pull DB -> Save CSV

        // ------------------------------------------------------------

        private async void btnPull_Click(object? sender, EventArgs e) {

            try {

                var cfg = ConfigStore.Load();

                var lineNumbers = BuildLineNumbersForSql(cfg.Airplanes);

                if (lineNumbers.Count == 0) {

                    MessageBox.Show("No VH/VZ values configured. Add rows first.", "PomReport",

                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    return;

                }

                if (string.IsNullOrWhiteSpace(cfg.ConnectionString)) {

                    MessageBox.Show("Missing connectionString in config.json (next to the EXE).", "PomReport",

                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return;

                }

                var exportDir = Path.Combine(AppContext.BaseDirectory, cfg.ExportFolderName ?? "exports");

                Directory.CreateDirectory(exportDir);

                Log("Running SQL pull...");

                Log($"Export folder: {exportDir}");

                Log($"LineNumbers: {lineNumbers.Count}");

                using var cts = new CancellationTokenSource();

                var outFile = await SqlJobSource.PullToCsvAsync(

                    cfg.ConnectionString,

                    SqlQueries.MainQuery,

                    lineNumbers,

                    exportDir,

                    string.IsNullOrWhiteSpace(cfg.ShopName) ? "PomReport" : cfg.ShopName,

                    ct: cts.Token

                );

                Log($"CSV written: {outFile}");

                MessageBox.Show($"Done.\n\n{outFile}", "PomReport",

                    MessageBoxButtons.OK, MessageBoxIcon.Information);

            }

            catch (Exception ex) {

                Log(ex.ToString());

                MessageBox.Show(ex.Message, "PomReport - Pull Error",

                    MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        // ------------------------------------------------------------

        // BUTTON: Test Pipeline (Fake NewData -> Baseline(4h) -> Compare -> CleanData)

        // Writes outputs under BIN\data for quick testing.

        // ------------------------------------------------------------

        private async void btnTestPipeline_Click(object sender, EventArgs e) {
            // Ensure Plan Sort template exists for the shop
            var configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "config");
            var planSortPath = Path.Combine(configDir, "plan_sort.csv");
            var planSortRepo = new PomReportCore.Services.PlanSortRepository(planSortPath);
            planSortRepo.WriteTemplate(overwrite: false);


            try {

                var baseDir = Path.Combine(AppContext.BaseDirectory, "data");

                var snapDir = Path.Combine(baseDir, "snapshots");

                var cfgDir = Path.Combine(baseDir, "config");

                Directory.CreateDirectory(snapDir);

                Directory.CreateDirectory(cfgDir);

                var lineStatusPath = Path.Combine(cfgDir, "line_status.json");

                var jobSortPath = Path.Combine(cfgDir, "job_sort.json");

                var lsRepo = new LineStatusRepository(lineStatusPath);

                if (!File.Exists(lineStatusPath))

                    await lsRepo.SaveAsync(FakeDataFactory.LineStatus());

                var jsRepo = new JobSortRepository(jobSortPath);

                if (!File.Exists(jobSortPath))

                    await jsRepo.SaveAsync(FakeDataFactory.JobSortRules());

                var lineStatus = await lsRepo.LoadAsync();

                var jobSortMap = await jsRepo.LoadMapAsync();

                var airplaneToLs = new Dictionary<string, LineStatusEntry>(StringComparer.OrdinalIgnoreCase);

                foreach (var e2 in lineStatus) {

                    airplaneToLs[e2.VH] = e2;

                    airplaneToLs[e2.VZ] = e2;

                }

                var store = new SnapshotStore(snapDir);

                // Baseline older than 4 hours

                var baseline = FakeDataFactory.BaselineSnapshot();

                await store.SaveSnapshotAsync(baseline);

                var current = FakeDataFactory.CurrentSnapshot();

                await store.SaveSnapshotAsync(current);

                var baselineWindow = TimeSpan.FromHours(4);

                var chosenBaseline = await store.GetBaselineSnapshotAsync(baselineWindow, DateTimeOffset.UtcNow);

                var compare = new CompareEngine()

                    .Compare(current, chosenBaseline, jobSortMap);

                var clean = new CleanDataBuilder()

                    .Build(current, compare, jobSortMap, airplaneToLs);

                var outPath = Path.Combine(baseDir, "clean_preview.json");

                await File.WriteAllTextAsync(

                    outPath,

                    JsonSerializer.Serialize(clean, new JsonSerializerOptions { WriteIndented = true })

                );

                var msg =

                    $"Pipeline OK\r\n\r\n" +

                    $"New: {compare.NewJobs.Count}\r\n" +

                    $"Completed: {compare.CompletedJobs.Count}\r\n" +

                    $"Comment Changed: {compare.CommentChangedJobs.Count}\r\n" +

                    $"Sold: {compare.SoldJobs.Count}\r\n\r\n" +

                    $"Uncategorized JobNumbers: {compare.UncategorizedJobNumbers.Count}\r\n" +

                    $"Groups: {clean.Groups.Count}\r\n" +

                    $"SellCount: {clean.SellCount}\r\n\r\n" +

                    $"Wrote: {outPath}";

                Log(msg);

                MessageBox.Show(msg, "Pipeline Test");

            }

            catch (Exception ex) {

                Log(ex.ToString());

                MessageBox.Show(ex.Message, "Pipeline Test Error");

            }

        }

        private void Log(string message) {

            _log.AppendText(message + Environment.NewLine);

        }

    }

}

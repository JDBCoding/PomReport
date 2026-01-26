using System;

using System.Collections.Generic;

using System.ComponentModel;

using System.IO;

using System.Linq;

using System.Text.Json;

using System.Windows.Forms;

namespace PomReport.App

{

    public partial class Form1 : Form

    {

        // ----- UI we build at runtime (table + add/remove only) -----

        private DataGridView _grid = null!;

        private TextBox _txtVH = null!;

        private TextBox _txtVZ = null!;

        private TextBox _txtLocation = null!;

        private Button _btnAdd = null!;

        private Button _btnRemove = null!;

        // ----- Data backing the grid -----

        private readonly BindingList<AirplaneEntry> _entries = new();

        // ----- Config file: next to EXE -----

        private static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "config.json");

        // ----- JSON options (matches your existing config style) -----

        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions

        {

            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

            WriteIndented = true

        };

        public Form1()

        {

            InitializeComponent();

            // Make sure the Pull button actually fires (designer wiring can get messed up)

            btnPull.Click -= btnPull_Click;

            btnPull.Click += btnPull_Click;

            BuildAirplaneTableUi();

            LoadConfigIntoGrid();

            RefreshSqlPreviewOnly();

        }

        // ============================================================

        //  Pull button

        // ============================================================

        private void btnPull_Click(object? sender, EventArgs e)

        {

            try

            {

                SaveGridToConfig();          // persist whatever is in the table

                LoadConfigIntoGrid();        // reload (keeps us honest)

                RefreshSqlPreviewOnly();     // show IN() and list

                // NEXT: This is where your DB pull + CSV save goes.

                // Right now we’re just proving the correct VH/VZ list is being passed.

            }

            catch (Exception ex)

            {

                MessageBox.Show(ex.Message, "PomReport", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        // ============================================================

        //  Build IN-list: jl.LineNumber IN ('VH110','VZ475',...)

        // ============================================================

        private List<string> BuildLineNumberFilter()

        {

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in _entries)

            {

                var vh = (e.Vh ?? "").Trim().ToUpperInvariant();

                var vz = (e.Vz ?? "").Trim().ToUpperInvariant();

                if (!string.IsNullOrWhiteSpace(vh)) set.Add(vh);

                if (!string.IsNullOrWhiteSpace(vz)) set.Add(vz);

            }

            return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        }

        private void RefreshSqlPreviewOnly()

        {

            _log.Clear();

            _log.AppendText($"Config: {ConfigPath}{Environment.NewLine}");

            var lineNumbers = BuildLineNumberFilter();

            // show line numbers list (like your Excel “Query” tab B2 helper)

            textLineNumbers.Text = string.Join(Environment.NewLine, lineNumbers);

            var inListSql = lineNumbers.Count == 0

                ? "('NONE')" // safe default; prevents SQL syntax error

                : "(" + string.Join(", ", lineNumbers.Select(x => $"'{x.Replace("'", "''")}'")) + ")";

            textQueryPreview.Text =

                "-- SQL Preview (tail)\r\n" +

                "AND jl.LineNumber IN " + inListSql + "\r\n";

            _log.AppendText($"Total LineNumbers: {lineNumbers.Count}{Environment.NewLine}");

        }

        // ============================================================

        //  UI: table + add/remove only

        // ============================================================

        private void BuildAirplaneTableUi()

        {

            // Put our custom UI at the top.

            // Your existing controls (btnPull/textLineNumbers/textQueryPreview/_log) stay as-is below.

            var panel = new TableLayoutPanel

            {

                Dock = DockStyle.Top,

                AutoSize = true,

                ColumnCount = 1,

                RowCount = 2,

                Padding = new Padding(8),

            };

            var addRow = new FlowLayoutPanel

            {

                Dock = DockStyle.Top,

                AutoSize = true,

                WrapContents = false

            };

            addRow.Controls.Add(new Label { Text = "VH:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });

            _txtVH = new TextBox { Width = 90 };

            addRow.Controls.Add(_txtVH);

            addRow.Controls.Add(new Label { Text = "VZ:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });

            _txtVZ = new TextBox { Width = 90 };

            addRow.Controls.Add(_txtVZ);

            addRow.Controls.Add(new Label { Text = "Location:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });

            _txtLocation = new TextBox { Width = 160 };

            addRow.Controls.Add(_txtLocation);

            _btnAdd = new Button { Text = "Add", Width = 90, Height = 28, Margin = new Padding(12, 2, 0, 0) };

            _btnAdd.Click += (_, __) => AddRow();

            addRow.Controls.Add(_btnAdd);

            _btnRemove = new Button { Text = "Remove Selected", Width = 150, Height = 28, Margin = new Padding(8, 2, 0, 0) };

            _btnRemove.Click += (_, __) => RemoveSelectedRow();

            addRow.Controls.Add(_btnRemove);

            _grid = new DataGridView

            {

                Dock = DockStyle.Top,

                Height = 170,

                AutoGenerateColumns = false,

                AllowUserToAddRows = false,

                AllowUserToDeleteRows = false,

                AllowUserToResizeRows = false,

                ReadOnly = true, // add/remove only

                SelectionMode = DataGridViewSelectionMode.FullRowSelect,

                MultiSelect = false

            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn

            {

                DataPropertyName = nameof(AirplaneEntry.Vh),

                HeaderText = "VH",

                Width = 90

            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn

            {

                DataPropertyName = nameof(AirplaneEntry.Vz),

                HeaderText = "VZ",

                Width = 90

            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn

            {

                DataPropertyName = nameof(AirplaneEntry.Location),

                HeaderText = "Location",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            });

            _grid.DataSource = _entries;

            panel.Controls.Add(addRow);

            panel.Controls.Add(_grid);

            Controls.Add(panel);

            panel.BringToFront();

        }

        private void AddRow()

        {

            var vh = (_txtVH.Text ?? "").Trim().ToUpperInvariant();

            var vz = (_txtVZ.Text ?? "").Trim().ToUpperInvariant();

            var loc = (_txtLocation.Text ?? "").Trim();

            // boom shop case: VZ-only is allowed

            if (string.IsNullOrWhiteSpace(vh) && string.IsNullOrWhiteSpace(vz))

            {

                MessageBox.Show("Enter at least VH or VZ.\n\nBoom shop rows can be VZ only.",

                    "PomReport", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;

            }

            // prevent duplicate exact pair (VH+VZ)

            bool exists = _entries.Any(x =>

                string.Equals((x.Vh ?? "").Trim(), vh, StringComparison.OrdinalIgnoreCase) &&

                string.Equals((x.Vz ?? "").Trim(), vz, StringComparison.OrdinalIgnoreCase));

            if (exists)

            {

                MessageBox.Show("That VH/VZ pair already exists.", "PomReport",

                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;

            }

            _entries.Add(new AirplaneEntry { Vh = vh, Vz = vz, Location = loc });

            _txtVH.Clear();

            _txtVZ.Clear();

            _txtLocation.Clear();

            SaveGridToConfig();

            RefreshSqlPreviewOnly();

        }

        private void RemoveSelectedRow()

        {

            if (_grid.CurrentRow?.DataBoundItem is not AirplaneEntry selected)

                return;

            // true delete

            _entries.Remove(selected);

            SaveGridToConfig();

            RefreshSqlPreviewOnly();

        }

        // ============================================================

        //  Config read/write (portable: stored next to EXE)

        // ============================================================

        private void LoadConfigIntoGrid()

        {

            var cfg = LoadConfig();

            _entries.RaiseListChangedEvents = false;

            _entries.Clear();

            foreach (var a in cfg.Airplanes)

                _entries.Add(new AirplaneEntry { Vh = a.Vh, Vz = a.Vz, Location = a.Location });

            _entries.RaiseListChangedEvents = true;

            _entries.ResetBindings();

        }

        private void SaveGridToConfig()

        {

            var cfg = LoadConfig();

            cfg.Airplanes = _entries

                .Select(x => new AirplaneEntry

                {

                    Vh = (x.Vh ?? "").Trim().ToUpperInvariant(),

                    Vz = (x.Vz ?? "").Trim().ToUpperInvariant(),

                    Location = (x.Location ?? "").Trim()

                })

                .ToList();

            SaveConfig(cfg);

        }

        private static RootConfig LoadConfig()

        {

            if (!File.Exists(ConfigPath))

            {

                var fresh = new RootConfig

                {

                    ShopName = "New Shop",

                    ExportFolderName = "exports",

                    SnapshotFolderName = "snapshots",

                    Airplanes = new List<AirplaneEntry>(),

                    JobCategories = new List<JobCategory>()

                };

                SaveConfig(fresh);

                return fresh;

            }

            var json = File.ReadAllText(ConfigPath);

            var cfg = JsonSerializer.Deserialize<RootConfig>(json, JsonOpts);

            if (cfg == null) throw new InvalidOperationException("config.json is empty or invalid.");

            cfg.Airplanes ??= new List<AirplaneEntry>();

            cfg.JobCategories ??= new List<JobCategory>();

            cfg.ShopName ??= "New Shop";

            cfg.ExportFolderName ??= "exports";

            cfg.SnapshotFolderName ??= "snapshots";

            return cfg;

        }

        private static void SaveConfig(RootConfig cfg)

        {

            var json = JsonSerializer.Serialize(cfg, JsonOpts);

            File.WriteAllText(ConfigPath, json);

        }

        // ============================================================

        //  Local DTOs (so we don't depend on conflicting namespaces)

        // ============================================================

        private sealed class RootConfig

        {

            public string? ShopName { get; set; }

            public string? ExportFolderName { get; set; }

            public string? SnapshotFolderName { get; set; }

            public List<AirplaneEntry> Airplanes { get; set; } = new();

            public List<JobCategory> JobCategories { get; set; } = new();

            public string? LastUpdatedUtc { get; set; }

        }

        private sealed class AirplaneEntry

        {

            public string? Vh { get; set; }

            public string? Vz { get; set; }

            public string? Location { get; set; }

        }

        private sealed class JobCategory

        {

            public string? Ip { get; set; }

            public string? Category { get; set; }

        }

    }

}

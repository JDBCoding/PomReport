using System;

using System.Collections.Generic;

using System.ComponentModel;

using System.Linq;

using System.Windows.Forms;

using PomReport.Config;

using PomReport.Config.Models;

namespace PomReport.App

{

    public partial class Form1 : Form

    {

        // ---- UI we create in code (no designer edits required) ----

        private DataGridView _grid = null!;

        private TextBox _txtVh = null!;

        private TextBox _txtVz = null!;

        private TextBox _txtLocation = null!;

        private Button _btnAdd = null!;

        private Button _btnRemove = null!;

        // ---- data binding ----

        private readonly BindingList<AirplanePair> _pairs = new();

        public Form1()

        {

            InitializeComponent();

            BuildAirplanePairsUi();

            LoadConfigIntoGridAndPreview();

        }

        // This method MUST exist because Form1.Designer.cs is wired to it.

        // We keep it async-safe (no blocking UI later when DB pull is wired).

        private async void btnPull_Click(object sender, EventArgs e)

        {

            try

            {

                // 1) Ensure config exists (first run -> setup)

                if (!ConfigStore.Exists())

                {

                    // If you still want SetupForm flow, keep it.

                    // Otherwise, you can auto-create default.

                    using var setup = new SetupForm();

                    if (setup.ShowDialog(this) != DialogResult.OK)

                        return;

                }

                // 2) Reload latest config (in case user just added/removed)

                LoadConfigIntoGridAndPreview();

                // 3) For now: just confirm what will be passed to SQL

                // (DB work comes next step)

                await System.Threading.Tasks.Task.CompletedTask;

            }

            catch (Exception ex)

            {

                MessageBox.Show(ex.Message, "PomReport", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        // ----------------------------

        // UI + persistence (table + add/remove only)

        // ----------------------------

        private void BuildAirplanePairsUi()

        {

            // We’ll add a small “Pairs” section at the TOP of the form.

            // It won’t break your existing controls; it just adds above them.

            var root = new TableLayoutPanel

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

            _grid = new DataGridView

            {

                Dock = DockStyle.Top,

                Height = 180,

                AutoGenerateColumns = false,

                AllowUserToAddRows = false,

                AllowUserToDeleteRows = false,

                AllowUserToResizeRows = false,

                ReadOnly = true, // add/remove ONLY

                SelectionMode = DataGridViewSelectionMode.FullRowSelect,

                MultiSelect = false

            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn

            {

                DataPropertyName = nameof(AirplanePair.Vh),

                HeaderText = "VH",

                Width = 90

            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn

            {

                DataPropertyName = nameof(AirplanePair.Vz),

                HeaderText = "VZ",

                Width = 90

            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn

            {

                DataPropertyName = nameof(AirplanePair.Location),

                HeaderText = "Location",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            });

            _grid.DataSource = _pairs;

            root.Controls.Add(addRow);

            root.Controls.Add(_grid);

            // Put it at the top of your form.

            Controls.Add(root);

            root.BringToFront();

        }

        private void LoadConfigIntoGridAndPreview()

        {

            // Ensure config exists if you want “portable” default behavior:

            // If SetupForm already created it, this is safe too.

            if (!ConfigStore.Exists())

            {

                // Don’t auto-create silently if you prefer the setup dialog.

                // But this makes dev/testing painless.

                ConfigStore.CreateDefaultIfMissing();

            }

            var cfg = ConfigStore.Load();

            // Load grid

            _pairs.RaiseListChangedEvents = false;

            _pairs.Clear();

            if (cfg.Airplanes != null)

            {

                foreach (var p in cfg.Airplanes)

                    _pairs.Add(p);

            }

            _pairs.RaiseListChangedEvents = true;

            _pairs.ResetBindings();

            // Build the IN-list values (THIS is the key fix: pass BOTH VH and VZ to jl.LineNumber)

            var lineNumbers = BuildLineNumbersForSql(cfg.Airplanes);

            // Update your existing log + preview areas (you said _log works)

            if (_log != null)

            {

                _log.Clear();

                _log.AppendText($"Shop: {cfg.ShopName}{Environment.NewLine}");

                _log.AppendText($"Config: {ConfigStore.ConfigPath}{Environment.NewLine}{Environment.NewLine}");

                _log.AppendText("LineNumbers passed to SQL (jl.LineNumber IN (...)):" + Environment.NewLine);

                foreach (var ln in lineNumbers)

                    _log.AppendText(ln + Environment.NewLine);

                _log.AppendText(Environment.NewLine + $"Total LineNumbers: {lineNumbers.Count}" + Environment.NewLine);

            }

            if (textQueryPreview != null)

            {

                var inList = lineNumbers.Count == 0

                    ? "()"

                    : "(" + string.Join(", ", lineNumbers.Select(x => $"'{x}'")) + ")";

                textQueryPreview.Text =

                    "-- SQL Preview\r\n" +

                    "AND jl.LineNumber IN " + inList + "\r\n";

            }

        }

        private static List<string> BuildLineNumbersForSql(IList<AirplanePair>? pairs)

        {

            if (pairs == null || pairs.Count == 0)

                return new List<string>();

            // Rule:

            // - Include VH values (if present)

            // - Include VZ values (if present)

            // - Boom shop case: VZ may exist with blank VH -> still included

            // - De-dupe + stable ordering

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in pairs)

            {

                var vh = (p.Vh ?? "").Trim();

                var vz = (p.Vz ?? "").Trim();

                if (!string.IsNullOrWhiteSpace(vh)) set.Add(vh);

                if (!string.IsNullOrWhiteSpace(vz)) set.Add(vz);

            }

            return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        }

        private void AddPair()

        {

            var vh = (_txtVh.Text ?? "").Trim();

            var vz = (_txtVz.Text ?? "").Trim();

            var loc = (_txtLocation.Text ?? "").Trim();

            // Enforce “add/remove only”, but we still validate new entries

            if (string.IsNullOrWhiteSpace(vh) && string.IsNullOrWhiteSpace(vz))

            {

                MessageBox.Show("Enter at least VH or VZ.\n\nBoom shop rows can be VZ only.", "PomReport",

                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;

            }

            // Optional: normalize casing

            vh = vh.ToUpperInvariant();

            vz = vz.ToUpperInvariant();

            // Prevent duplicates (same exact VH+VZ)

            var exists = _pairs.Any(p =>

                string.Equals((p.Vh ?? "").Trim(), vh, StringComparison.OrdinalIgnoreCase) &&

                string.Equals((p.Vz ?? "").Trim(), vz, StringComparison.OrdinalIgnoreCase));

            if (exists)

            {

                MessageBox.Show("That VH/VZ pair already exists.", "PomReport",

                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;

            }

            var cfg = ConfigStore.Load();

            cfg.Airplanes ??= new List<AirplanePair>();

            cfg.Airplanes.Add(new AirplanePair

            {

                Vh = vh,

                Vz = vz,

                Location = loc

            });

            ConfigStore.Save(cfg);

            // refresh UI + IN-list preview

            _txtVh.Clear();

            _txtVz.Clear();

            _txtLocation.Clear();

            LoadConfigIntoGridAndPreview();

        }

        private void RemoveSelectedPair()

        {

            if (_grid.CurrentRow == null)

                return;

            if (_grid.CurrentRow.DataBoundItem is not AirplanePair selected)

                return;

            var cfg = ConfigStore.Load();

            if (cfg.Airplanes == null || cfg.Airplanes.Count == 0)

                return;

            // true delete (per your requirement)

            var removed = cfg.Airplanes.RemoveAll(p =>

                string.Equals((p.Vh ?? "").Trim(), (selected.Vh ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&

                string.Equals((p.Vz ?? "").Trim(), (selected.Vz ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&

                string.Equals((p.Location ?? "").Trim(), (selected.Location ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

            if (removed > 0)

                ConfigStore.Save(cfg);

            LoadConfigIntoGridAndPreview();

        }

    }

}

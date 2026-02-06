using System;

using System.ComponentModel;

using System.IO;

using System.Linq;

using System.Collections.Generic;

using System.Windows.Forms;

using PomReport.Config;

using PomReport.Config.Models;

using PomReport.Core.Services;

namespace PomReport.App {

    public partial class SetupForm : Form {

        private TextBox txtShop = null!;

        private DataGridView dgvAirplanes = null!;

        private TextBox txtCategories = null!;

        private TextBox txtJobMappingPath = null!;

        private DataGridView dgvJobMapping = null!;

        private Button btnSave = null!;

        private Button btnCancel = null!;

        private BindingList<AirplanePair> _airplanes = new();

        private ShopConfig _existing = new();

        public SetupForm() {

            InitializeComponent();

            LoadConfigIntoUi();

            InitJobMappingUi();

        }

        private static JobMappingRepository CreateJobMapRepo() {

            var path = JobMappingRepository.GetDefaultPath();

            return new JobMappingRepository(path);

        }

        private void InitializeComponent() {

            Text = "Setup";

            Width = 980;

            Height = 980;

            StartPosition = FormStartPosition.CenterScreen;

            var lblShop = new Label { Left = 15, Top = 15, Width = 200, Text = "Shop Name" };

            txtShop = new TextBox { Left = 15, Top = 40, Width = 400 };

            var lblAir = new Label {

                Left = 15,

                Top = 80,

                Width = 900,

                Text = "Airplanes (edit here; Line Number is shop-only and never sent to SQL)"

            };

            dgvAirplanes = new DataGridView {

                Left = 15,

                Top = 105,

                Width = 930,

                Height = 330,

                AllowUserToAddRows = true,

                AllowUserToDeleteRows = true,

                AutoGenerateColumns = false,

                MultiSelect = false,

                SelectionMode = DataGridViewSelectionMode.CellSelect,

                RowHeadersVisible = false

            };

            dgvAirplanes.Columns.Add(new DataGridViewTextBoxColumn {

                HeaderText = "Line Number",

                DataPropertyName = "LineNumber",

                Name = "colLineNumber",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells

            });

            dgvAirplanes.Columns.Add(new DataGridViewTextBoxColumn {

                HeaderText = "VH",

                DataPropertyName = "Vh",

                Name = "colVh",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells

            });

            dgvAirplanes.Columns.Add(new DataGridViewTextBoxColumn {

                HeaderText = "VZ",

                DataPropertyName = "Vz",

                Name = "colVz",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells

            });

            dgvAirplanes.Columns.Add(new DataGridViewTextBoxColumn {

                HeaderText = "Location",

                DataPropertyName = "Location",

                Name = "colLocation",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            });

            var lblCat = new Label {

                Left = 15,

                Top = 450,

                Width = 900,

                Text = "Job Category Mapping (one per line). Format: IP100=Hydraulics"

            };

            txtCategories = new TextBox {

                Left = 15,

                Top = 475,

                Width = 930,

                Height = 120,

                Multiline = true,

                ScrollBars = ScrollBars.Vertical

            };

            // Job mapping controls (wired up in InitJobMappingUi)

            var lblJobMap = new Label {

                Left = 15,

                Top = 605,

                Width = 930,

                Text = "Job Mapping (keyed by JobKit). CSV columns: JobKit,Category,DisplayName,SortOrder,Notes"

            };

            txtJobMappingPath = new TextBox {

                Left = 15,

                Top = 630,

                Width = 650,

                ReadOnly = true

            };

            var btnOpenOrCreateJobMap = new Button { Left = 675, Top = 628, Width = 130, Text = "Open/Create" };

            var btnLoadJobMap = new Button { Left = 815, Top = 628, Width = 130, Text = "Load CSV..." };

            dgvJobMapping = new DataGridView {

                Left = 15,

                Top = 660,

                Width = 930,

                Height = 210,

                AllowUserToAddRows = false,

                AllowUserToDeleteRows = false,

                ReadOnly = true,

                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,

                RowHeadersVisible = false

            };

            btnSave = new Button { Left = 15, Top = 890, Width = 160, Text = "Save" };

            btnCancel = new Button { Left = 190, Top = 890, Width = 160, Text = "Cancel" };

            btnSave.Click += BtnSave_Click;

            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(lblShop);

            Controls.Add(txtShop);

            Controls.Add(lblAir);

            Controls.Add(dgvAirplanes);

            Controls.Add(lblCat);

            Controls.Add(txtCategories);

            Controls.Add(lblJobMap);

            Controls.Add(txtJobMappingPath);

            Controls.Add(btnOpenOrCreateJobMap);

            Controls.Add(btnLoadJobMap);

            Controls.Add(dgvJobMapping);

            Controls.Add(btnSave);

            Controls.Add(btnCancel);

            // Hook events here, but implemented using repo methods that exist.

            btnOpenOrCreateJobMap.Click += (s, e) => {

                var repo = CreateJobMapRepo();
                repo.EnsureTemplateExists();
                try {
                    var src = ofd.FileName;
                    var dest = repo.FilePath;
                    // If they picked the canonical file itself, do nothing.
                    if (!string.Equals(
                            Path.GetFullPath(src),
                            Path.GetFullPath(dest),
                            StringComparison.OrdinalIgnoreCase)) {
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        File.Copy(src, dest, overwrite: true);
                    }
                    txtJobMappingPath.Text = repo.FilePath;
                    LoadJobMappingGrid(repo);
                }
                catch (IOException ioEx) {
                    MessageBox.Show(
                        "Couldn't update job_mapping.csv because it is locked (usually Excel).\n\n" +
                        "Close the CSV (and any Excel window using it), then try again.\n\n" +
                        ioEx.Message,
                        "Job Mapping CSV Locked",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch (Exception ex) {
                    MessageBox.Show(ex.Message, "Job Mapping Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            };

        }

        private void InitJobMappingUi() {

            // Populate path + grid on open

            try {

                var repo = CreateJobMapRepo();

                repo.EnsureTemplateExists();

                txtJobMappingPath.Text = repo.FilePath;

                LoadJobMappingGrid(repo);

            }

            catch {

                // Keep setup usable even if job mapping CSV has issues.

            }

        }

        private void LoadJobMappingGrid(JobMappingRepository repo) {

            try {

                var rules = repo.Load();

                dgvJobMapping.Columns.Clear();

                dgvJobMapping.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "JobKit", DataPropertyName = "JobKit" });

                dgvJobMapping.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category", DataPropertyName = "Category" });

                dgvJobMapping.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DisplayName", DataPropertyName = "DisplayName" });

                dgvJobMapping.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SortOrder", DataPropertyName = "SortOrder" });

                dgvJobMapping.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Notes", DataPropertyName = "Notes" });

                // Sort behavior:

                // Category first, then SortOrder within that category, then JobKit as a stable tiebreaker.

                dgvJobMapping.DataSource = rules

                    .OrderBy(r => r.Category ?? "", StringComparer.OrdinalIgnoreCase)

                    .ThenBy(r => r.SortOrder)

                    .ThenBy(r => r.JobKit ?? "", StringComparer.OrdinalIgnoreCase)

                    .Select(r => new {

                        r.JobKit,

                        r.Category,

                        r.DisplayName,

                        r.SortOrder,

                        r.Notes

                    })

                    .ToList();

            }

            catch {

                // If the CSV is malformed, don't crash setup UI.

            }

        }

        private void LoadConfigIntoUi() {

            ConfigStore.CreateDefaultIfMissing();

            _existing = ConfigStore.Load();

            txtShop.Text = _existing.ShopName ?? "";

            _airplanes = new BindingList<AirplanePair>(

                (_existing.Airplanes ?? new List<AirplanePair>())

                .Select(a => new AirplanePair {

                    LineNumber = (a.LineNumber ?? "").Trim(),

                    Vh = (a.Vh ?? "").Trim(),

                    Vz = (a.Vz ?? "").Trim(),

                    Location = (a.Location ?? "").Trim()

                })

                .ToList()

            );

            dgvAirplanes.DataSource = _airplanes;

            txtCategories.Text = string.Join(

                Environment.NewLine,

                (_existing.JobCategories ?? new List<JobCategoryMap>())

                    .Select(c => $"{(c.Ip ?? "").Trim()}={(c.Category ?? "").Trim()}")

                    .Where(s => !string.IsNullOrWhiteSpace(s) && s != "=")

            );

        }

        private void BtnSave_Click(object? sender, EventArgs e) {

            try {

                var cfg = _existing ?? new ShopConfig();

                cfg.ShopName = (txtShop.Text ?? "").Trim();

                cfg.Airplanes = _airplanes

                    .Select(a => new AirplanePair {

                        LineNumber = (a.LineNumber ?? "").Trim(),

                        Vh = (a.Vh ?? "").Trim(),

                        Vz = (a.Vz ?? "").Trim(),

                        Location = (a.Location ?? "").Trim()

                    })

                    .Where(a =>

                        !string.IsNullOrWhiteSpace(a.LineNumber) ||

                        !string.IsNullOrWhiteSpace(a.Vh) ||

                        !string.IsNullOrWhiteSpace(a.Vz) ||

                        !string.IsNullOrWhiteSpace(a.Location))

                    .ToList();

                cfg.JobCategories = (txtCategories.Text ?? "")

                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)

                    .Select(line => line.Trim())

                    .Where(line => !string.IsNullOrWhiteSpace(line) && line.Contains('='))

                    .Select(line => {

                        var parts = line.Split(new[] { '=' }, 2);

                        return new JobCategoryMap {

                            Ip = parts[0].Trim(),

                            Category = parts.Length > 1 ? parts[1].Trim() : ""

                        };

                    })

                    .Where(m => !string.IsNullOrWhiteSpace(m.Ip))

                    .ToList();

                ConfigStore.Save(cfg);

                DialogResult = DialogResult.OK;

                Close();

            }

            catch (Exception ex) {

                MessageBox.Show(ex.Message, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

    }

}

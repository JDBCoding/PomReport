using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using PomReport.Config;
using PomReport.Config.Models;
namespace PomReport.App
{
   public partial class SetupForm : Form
   {
       private TextBox txtShop = null!;
       private DataGridView dgvAirplanes = null!;
       private TextBox txtCategories = null!;
       private Button btnSave = null!;
       private Button btnCancel = null!;
       private BindingList<AirplanePair> _airplanes = new();
       private ShopConfig _existing = new();
       public SetupForm()
       {
           InitializeComponent();
           LoadConfigIntoUi();
       }
       private void InitializeComponent()
       {
           Text = "Setup";
           Width = 980;
           Height = 760;
           StartPosition = FormStartPosition.CenterScreen;
           var lblShop = new Label { Left = 15, Top = 15, Width = 200, Text = "Shop Name" };
           txtShop = new TextBox { Left = 15, Top = 40, Width = 400 };
           var lblAir = new Label
           {
               Left = 15,
               Top = 80,
               Width = 900,
               Text = "Airplanes (edit here; Line Number is shop-only and never sent to SQL)"
           };
           dgvAirplanes = new DataGridView
           {
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
           // Column A: Line Number (shop-only)
           dgvAirplanes.Columns.Add(new DataGridViewTextBoxColumn
           {
               HeaderText = "Line Number",
               DataPropertyName = "LineNumber",
               Name = "colLineNumber",
               AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
           });
           // Column B: VH
           dgvAirplanes.Columns.Add(new DataGridViewTextBoxColumn
           {
               HeaderText = "VH",
               DataPropertyName = "Vh",
               Name = "colVh",
               AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
           });
           // Column C: VZ
           dgvAirplanes.Columns.Add(new DataGridViewTextBoxColumn
           {
               HeaderText = "VZ",
               DataPropertyName = "Vz",
               Name = "colVz",
               AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
           });
           // Column D: Location
           dgvAirplanes.Columns.Add(new DataGridViewTextBoxColumn
           {
               HeaderText = "Location",
               DataPropertyName = "Location",
               Name = "colLocation",
               AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
           });
           var lblCat = new Label
           {
               Left = 15,
               Top = 450,
               Width = 900,
               Text = "Job Category Mapping (one per line). Format: IP100=Hydraulics"
           };
           txtCategories = new TextBox
           {
               Left = 15,
               Top = 475,
               Width = 930,
               Height = 180,
               Multiline = true,
               ScrollBars = ScrollBars.Vertical
           };
           btnSave = new Button { Left = 15, Top = 670, Width = 160, Text = "Save" };
           btnCancel = new Button { Left = 190, Top = 670, Width = 160, Text = "Cancel" };
           btnSave.Click += BtnSave_Click;
           btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
           Controls.Add(lblShop);
           Controls.Add(txtShop);
           Controls.Add(lblAir);
           Controls.Add(dgvAirplanes);
           Controls.Add(lblCat);
           Controls.Add(txtCategories);
           Controls.Add(btnSave);
           Controls.Add(btnCancel);
       }
       private void LoadConfigIntoUi()
       {
           // Ensure config exists so SetupForm is always usable
           ConfigStore.CreateDefaultIfMissing();
           _existing = ConfigStore.Load();
           txtShop.Text = _existing.ShopName ?? "";
           // Load airplanes into BindingList for editing
           _airplanes = new BindingList<AirplanePair>(
               (_existing.Airplanes ?? new List<AirplanePair>())
               .Select(a => new AirplanePair
               {
                   LineNumber = (a.LineNumber ?? "").Trim(),
                   Vh = (a.Vh ?? "").Trim(),
                   Vz = (a.Vz ?? "").Trim(),
                   Location = (a.Location ?? "").Trim()
               })
               .ToList()
           );
           dgvAirplanes.DataSource = _airplanes;
           // Load categories into multiline textbox
           txtCategories.Text = string.Join(
               Environment.NewLine,
               (_existing.JobCategories ?? new List<JobCategoryMap>())
                   .Select(c => $"{(c.Ip ?? "").Trim()}={(c.Category ?? "").Trim()}")
                   .Where(s => !string.IsNullOrWhiteSpace(s) && s != "=")
           );
       }
       private void BtnSave_Click(object? sender, EventArgs e)
       {
           try
           {
               dgvAirplanes.EndEdit();
               var airplanes = CollectAndValidateAirplanes();
               var categories = ParseCategories(txtCategories.Text);
               // Preserve unrelated settings from existing config (connection string, folders, etc.)
               _existing.ShopName = (txtShop.Text ?? "").Trim();
               _existing.Airplanes = airplanes;
               _existing.JobCategories = categories;
               _existing.LastUpdatedUtc = DateTime.UtcNow;
               ConfigStore.Save(_existing);
               DialogResult = DialogResult.OK;
               Close();
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message, "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
           }
       }
       private List<AirplanePair> CollectAndValidateAirplanes()
       {
           var list = new List<AirplanePair>();
           foreach (var row in _airplanes)
           {
               // Treat empty rows as "skip" (common when user leaves last add-row blank)
               var ln = (row.LineNumber ?? "").Trim();
               var vh = (row.Vh ?? "").Trim();
               var vz = (row.Vz ?? "").Trim();
               var loc = (row.Location ?? "").Trim();
               if (string.IsNullOrWhiteSpace(ln) &&
                   string.IsNullOrWhiteSpace(vh) &&
                   string.IsNullOrWhiteSpace(vz) &&
                   string.IsNullOrWhiteSpace(loc))
               {
                   continue;
               }
               // Validation: must have VH or VZ; LineNumber optional
               if (string.IsNullOrWhiteSpace(vh) && string.IsNullOrWhiteSpace(vz))
                   throw new Exception("Each airplane row must have either VH or VZ (Line Number is optional).");
               list.Add(new AirplanePair
               {
                   LineNumber = ln,
                   Vh = vh,
                   Vz = vz,
                   Location = loc
               });
           }
           if (list.Count == 0)
               throw new Exception("No valid airplanes entered. Add at least one row with VH or VZ.");
           return list;
       }
       private static List<JobCategoryMap> ParseCategories(string text)
       {
           var list = new List<JobCategoryMap>();
           var lines = (text ?? "")
               .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
               .Select(l => l.Trim())
               .Where(l => !string.IsNullOrWhiteSpace(l));
           foreach (var line in lines)
           {
               var kv = line.Split('=');
               if (kv.Length != 2)
                   throw new Exception($"Invalid category line: {line}");
               var ip = kv[0].Trim();
               var cat = kv[1].Trim();
               if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(cat))
                   throw new Exception($"Invalid category line (missing value): {line}");
               list.Add(new JobCategoryMap { Ip = ip, Category = cat });
           }
           return list;
       }
   }
}
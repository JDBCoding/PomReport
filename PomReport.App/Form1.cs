using System;
using System.Collections.Generic;
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
       private FlowLayoutPanel? _rootPanel;
       private DataGridView? _grid;
       private TextBox? _txtVh;
       private TextBox? _txtVz;
       private TextBox? _txtLocation;
       private Button? _btnAdd;
       private Button? _btnRemove;
       public Form1()
       {
           InitializeComponent();
           BuildAirplaneEditorUi();
           // Load config into grid + preview on startup
           Load += (_, __) => LoadConfigIntoGridAndPreview();
       }
       // ------------------------------------------------------------
       // UI BUILD (Table + Add/Remove only)
       // ------------------------------------------------------------
       private void BuildAirplaneEditorUi()
       {
           _rootPanel = new FlowLayoutPanel
           {
               Dock = DockStyle.Top,
               Height = 220,
               AutoSize = false,
               WrapContents = false,
               FlowDirection = FlowDirection.TopDown,
               Padding = new Padding(12, 48, 12, 8) // leave room for btnPull at top
           };
           var addRow = new FlowLayoutPanel
           {
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
           _rootPanel.Controls.Add(addRow);
           _rootPanel.Controls.Add(_grid);
           Controls.Add(_rootPanel);
           _rootPanel.BringToFront();
       }
       // ------------------------------------------------------------
       // CONFIG LOAD/SAVE + PREVIEW
       // ------------------------------------------------------------
       private void LoadConfigIntoGridAndPreview()
       {
           try
           {
               if (!ConfigStore.Exists())
               {
                   ConfigStore.CreateDefaultIfMissing();
               }
               var cfg = ConfigStore.Load();
               _pairs.RaiseListChangedEvents = false;
               _pairs.Clear();
               if (cfg.Airplanes != null)
               {
                   foreach (var p in cfg.Airplanes)
                       _pairs.Add(p);
               }
               _pairs.RaiseListChangedEvents = true;
               _pairs.ResetBindings();
               var lineNumbers = BuildLineNumbersForSql(cfg.Airplanes);
               _log.Clear();
               _log.AppendText($"Config: {ConfigStore.ConfigPath}{Environment.NewLine}");
               _log.AppendText($"Airplanes rows: {_pairs.Count}{Environment.NewLine}");
               _log.AppendText($"LineNumbers passed to SQL: {lineNumbers.Count}{Environment.NewLine}{Environment.NewLine}");
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
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message, "PomReport - LoadConfig failed",
                   MessageBoxButtons.OK, MessageBoxIcon.Error);
           }
       }
       private static List<string> BuildLineNumbersForSql(IList<AirplanePair>? pairs)
       {
           if (pairs == null || pairs.Count == 0)
               return new List<string>();
           var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
           foreach (var p in pairs)
           {
               var vh = (p.Vh ?? "").Trim();
               var vz = (p.Vz ?? "").Trim();
               if (!string.IsNullOrWhiteSpace(vh)) set.Add(vh.ToUpperInvariant());
               if (!string.IsNullOrWhiteSpace(vz)) set.Add(vz.ToUpperInvariant());
           }
           return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
       }
       private void AddPair()
       {
           var vh = (_txtVh?.Text ?? "").Trim().ToUpperInvariant();
           var vz = (_txtVz?.Text ?? "").Trim().ToUpperInvariant();
           var loc = (_txtLocation?.Text ?? "").Trim();
           if (string.IsNullOrWhiteSpace(vh) && string.IsNullOrWhiteSpace(vz))
           {
               MessageBox.Show("Enter at least VH or VZ.\n\nBoom shop rows can be VZ only.",
                   "PomReport", MessageBoxButtons.OK, MessageBoxIcon.Information);
               return;
           }
           var cfg = ConfigStore.Load();
           cfg.Airplanes ??= new List<AirplanePair>();
           // prevent exact duplicates
           var exists = cfg.Airplanes.Any(p =>
               string.Equals((p.Vh ?? "").Trim(), vh, StringComparison.OrdinalIgnoreCase) &&
               string.Equals((p.Vz ?? "").Trim(), vz, StringComparison.OrdinalIgnoreCase));
           if (exists)
           {
               MessageBox.Show("That VH/VZ pair already exists.", "PomReport",
                   MessageBoxButtons.OK, MessageBoxIcon.Information);
               return;
           }
           cfg.Airplanes.Add(new AirplanePair
           {
               Vh = vh,
               Vz = vz,
               Location = loc
           });
           ConfigStore.Save(cfg);
           _txtVh?.Clear();
           _txtVz?.Clear();
           _txtLocation?.Clear();
           LoadConfigIntoGridAndPreview();
       }
       private void RemoveSelectedPair()
       {
           if (_grid?.CurrentRow == null)
               return;
           if (_grid.CurrentRow.DataBoundItem is not AirplanePair selected)
               return;
           var cfg = ConfigStore.Load();
           if (cfg.Airplanes == null || cfg.Airplanes.Count == 0)
               return;
           // true delete (your requirement)
           var removed = cfg.Airplanes.RemoveAll(p =>
               string.Equals((p.Vh ?? "").Trim(), (selected.Vh ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&
               string.Equals((p.Vz ?? "").Trim(), (selected.Vz ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&
               string.Equals((p.Location ?? "").Trim(), (selected.Location ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
           if (removed > 0)
               ConfigStore.Save(cfg);
           LoadConfigIntoGridAndPreview();
       }
       // ------------------------------------------------------------
       // BUTTON: Pull DB -> Save CSV
       // ------------------------------------------------------------
       private async void btnPull_Click(object? sender, EventArgs e)
       {
           try
           {
               var cfg = ConfigStore.Load();
               var lineNumbers = BuildLineNumbersForSql(cfg.Airplanes);
               if (lineNumbers.Count == 0)
               {
                   MessageBox.Show("No VH/VZ values configured. Add rows first.", "PomReport",
                       MessageBoxButtons.OK, MessageBoxIcon.Information);
                   return;
               }
               if (string.IsNullOrWhiteSpace(cfg.ConnectionString))
               {
                   MessageBox.Show("Missing connectionString in config.json (next to the EXE).", "PomReport",
                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                   return;
               }
               var exportDir = Path.Combine(AppContext.BaseDirectory, cfg.ExportFolderName ?? "exports");
               Directory.CreateDirectory(exportDir);
               Log($"Running SQL pull...");
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
               MessageBox.Show("Done.\n\n" + outFile, "PomReport",
                   MessageBoxButtons.OK, MessageBoxIcon.Information);
           }
           catch (Exception ex)
           {
               Log(ex.ToString());
               MessageBox.Show(ex.Message, "PomReport - Run failed",
                   MessageBoxButtons.OK, MessageBoxIcon.Error);
           }
       }
       private void Log(string message)
       {
           _log.AppendText(message + Environment.NewLine);
       }
   }
}
namespace PomReport.App;
partial class Form1
{
   private System.ComponentModel.IContainer components = null!;
   protected override void Dispose(bool disposing)
   {
       if (disposing && (components != null))
       {
           components.Dispose();
       }
       base.Dispose(disposing);
   }
   #region Windows Form Designer generated code
   private void InitializeComponent()
   {
       btnPull = new Button();
       textQueryPreview = new TextBox();
       txtVh = new TextBox();
       txtVz = new TextBox();
       txtLocation = new TextBox();
       lblVh = new Label();
       lblVz = new Label();
       lblLocation = new Label();
       btnAdd = new Button();
       btnRemoveSelected = new Button();
       dataGridAirplanes = new DataGridView();
       _log = new TextBox();
       ((System.ComponentModel.ISupportInitialize)dataGridAirplanes).BeginInit();
       SuspendLayout();
       // btnPull
       btnPull.Dock = DockStyle.Top;
       btnPull.Height = 34;
       btnPull.Name = "btnPull";
       btnPull.TabIndex = 0;
       btnPull.Text = "Pull from DB -> Save CSV";
       btnPull.UseVisualStyleBackColor = true;
       // textQueryPreview
       textQueryPreview.Dock = DockStyle.Top;
       textQueryPreview.Font = new Font("Consolas", 9F);
       textQueryPreview.Multiline = true;
       textQueryPreview.Height = 70;
       textQueryPreview.ReadOnly = true;
       textQueryPreview.ScrollBars = ScrollBars.Both;
       textQueryPreview.WordWrap = false;
       textQueryPreview.Name = "textQueryPreview";
       textQueryPreview.TabIndex = 1;
       // Labels + textboxes row
       lblVh.AutoSize = true;
       lblVh.Text = "VH:";
       lblVh.Location = new Point(12, 118);
       txtVh.Name = "txtVh";
       txtVh.Location = new Point(45, 114);
       txtVh.Size = new Size(130, 23);
       lblVz.AutoSize = true;
       lblVz.Text = "VZ:";
       lblVz.Location = new Point(190, 118);
       txtVz.Name = "txtVz";
       txtVz.Location = new Point(222, 114);
       txtVz.Size = new Size(130, 23);
       lblLocation.AutoSize = true;
       lblLocation.Text = "Location:";
       lblLocation.Location = new Point(365, 118);
       txtLocation.Name = "txtLocation";
       txtLocation.Location = new Point(430, 114);
       txtLocation.Size = new Size(280, 23);
       // btnAdd
       btnAdd.Name = "btnAdd";
       btnAdd.Text = "Add";
       btnAdd.Location = new Point(725, 112);
       btnAdd.Size = new Size(110, 27);
       // btnRemoveSelected
       btnRemoveSelected.Name = "btnRemoveSelected";
       btnRemoveSelected.Text = "Remove Selected";
       btnRemoveSelected.Location = new Point(845, 112);
       btnRemoveSelected.Size = new Size(140, 27);
       // dataGridAirplanes
       dataGridAirplanes.Name = "dataGridAirplanes";
       dataGridAirplanes.Location = new Point(12, 150);
       dataGridAirplanes.Size = new Size(1050, 260);
       dataGridAirplanes.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
       dataGridAirplanes.AllowUserToAddRows = false;
       dataGridAirplanes.AllowUserToDeleteRows = false;
       dataGridAirplanes.ReadOnly = true;
       dataGridAirplanes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
       dataGridAirplanes.MultiSelect = true;
       dataGridAirplanes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
       // _log
       _log.Name = "_log";
       _log.Multiline = true;
       _log.ScrollBars = ScrollBars.Vertical;
       _log.WordWrap = false;
       _log.Font = new Font("Consolas", 9F);
       _log.Location = new Point(12, 420);
       _log.Size = new Size(1050, 260);
       _log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
       // Form1
       AutoScaleDimensions = new SizeF(7F, 15F);
       AutoScaleMode = AutoScaleMode.Font;
       ClientSize = new Size(1080, 700);
       Controls.Add(_log);
       Controls.Add(dataGridAirplanes);
       Controls.Add(btnRemoveSelected);
       Controls.Add(btnAdd);
       Controls.Add(txtLocation);
       Controls.Add(lblLocation);
       Controls.Add(txtVz);
       Controls.Add(lblVz);
       Controls.Add(txtVh);
       Controls.Add(lblVh);
       Controls.Add(textQueryPreview);
       Controls.Add(btnPull);
       Name = "Form1";
       Text = "PomReport";
       ((System.ComponentModel.ISupportInitialize)dataGridAirplanes).EndInit();
       ResumeLayout(false);
       PerformLayout();
   }
   #endregion
   private Button btnPull;
   private TextBox textQueryPreview;
   private Label lblVh;
   private Label lblVz;
   private Label lblLocation;
   private TextBox txtVh;
   private TextBox txtVz;
   private TextBox txtLocation;
   private Button btnAdd;
   private Button btnRemoveSelected;
   private DataGridView dataGridAirplanes;
   private TextBox _log;
}
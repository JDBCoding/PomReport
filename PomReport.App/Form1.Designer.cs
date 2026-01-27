namespace PomReport.App;

partial class Form1

{

    private System.ComponentModel.IContainer components = null;

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

        lblVh = new Label();

        lblVz = new Label();

        lblLocation = new Label();

        txtVh = new TextBox();

        txtVz = new TextBox();

        txtLocation = new TextBox();

        btnAdd = new Button();

        btnRemoveSelected = new Button();

        dataGridAirplanes = new DataGridView();

        colVh = new DataGridViewTextBoxColumn();

        colVz = new DataGridViewTextBoxColumn();

        colLocation = new DataGridViewTextBoxColumn();

        _log = new TextBox();

        SuspendLayout();

        // btnPull

        btnPull.Dock = DockStyle.Top;

        btnPull.Height = 32;

        btnPull.Name = "btnPull";

        btnPull.TabIndex = 0;

        btnPull.Text = "Pull from DB -> Save CSV";

        btnPull.UseVisualStyleBackColor = true;

        // textQueryPreview

        textQueryPreview.Dock = DockStyle.Top;

        textQueryPreview.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);

        textQueryPreview.Multiline = true;

        textQueryPreview.Height = 80;

        textQueryPreview.Name = "textQueryPreview";

        textQueryPreview.ReadOnly = true;

        textQueryPreview.ScrollBars = ScrollBars.Both;

        textQueryPreview.TabIndex = 1;

        textQueryPreview.WordWrap = false;

        // Labels

        lblVh.AutoSize = true;

        lblVh.Text = "VH:";

        lblVh.Location = new Point(12, 128);

        lblVz.AutoSize = true;

        lblVz.Text = "VZ:";

        lblVz.Location = new Point(180, 128);

        lblLocation.AutoSize = true;

        lblLocation.Text = "Location:";

        lblLocation.Location = new Point(348, 128);

        // Textboxes

        txtVh.Location = new Point(48, 124);

        txtVh.Size = new Size(120, 23);

        txtVh.Name = "txtVh";

        txtVh.TabIndex = 2;

        txtVz.Location = new Point(216, 124);

        txtVz.Size = new Size(120, 23);

        txtVz.Name = "txtVz";

        txtVz.TabIndex = 3;

        txtLocation.Location = new Point(412, 124);

        txtLocation.Size = new Size(240, 23);

        txtLocation.Name = "txtLocation";

        txtLocation.TabIndex = 4;

        // btnAdd

        btnAdd.Location = new Point(670, 123);

        btnAdd.Size = new Size(90, 25);

        btnAdd.Name = "btnAdd";

        btnAdd.TabIndex = 5;

        btnAdd.Text = "Add";

        btnAdd.UseVisualStyleBackColor = true;

        // btnRemoveSelected

        btnRemoveSelected.Location = new Point(770, 123);

        btnRemoveSelected.Size = new Size(140, 25);

        btnRemoveSelected.Name = "btnRemoveSelected";

        btnRemoveSelected.TabIndex = 6;

        btnRemoveSelected.Text = "Remove Selected";

        btnRemoveSelected.UseVisualStyleBackColor = true;

        // dataGridAirplanes

        dataGridAirplanes.Location = new Point(12, 155);

        dataGridAirplanes.Size = new Size(898, 220);

        dataGridAirplanes.Name = "dataGridAirplanes";

        dataGridAirplanes.TabIndex = 7;

        dataGridAirplanes.AllowUserToAddRows = false;

        dataGridAirplanes.AllowUserToDeleteRows = false;

        dataGridAirplanes.ReadOnly = true;

        dataGridAirplanes.RowHeadersVisible = false;

        dataGridAirplanes.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        dataGridAirplanes.Columns.AddRange(new DataGridViewColumn[] { colVh, colVz, colLocation });

        // colVh

        colVh.HeaderText = "VH";

        colVh.DataPropertyName = "Vh";

        colVh.Width = 120;

        // colVz

        colVz.HeaderText = "VZ";

        colVz.DataPropertyName = "Vz";

        colVz.Width = 120;

        // colLocation

        colLocation.HeaderText = "Location";

        colLocation.DataPropertyName = "Location";

        colLocation.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        // _log

        _log.Location = new Point(12, 385);

        _log.Multiline = true;

        _log.Name = "_log";

        _log.ScrollBars = ScrollBars.Vertical;

        _log.WordWrap = false;

        _log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _log.Size = new Size(898, 260);

        _log.TabIndex = 8;

        // Form1

        AutoScaleDimensions = new SizeF(7F, 15F);

        AutoScaleMode = AutoScaleMode.Font;

        ClientSize = new Size(930, 660);

        Controls.Add(_log);

        Controls.Add(dataGridAirplanes);

        Controls.Add(btnRemoveSelected);

        Controls.Add(btnAdd);

        Controls.Add(txtLocation);

        Controls.Add(txtVz);

        Controls.Add(txtVh);

        Controls.Add(lblLocation);

        Controls.Add(lblVz);

        Controls.Add(lblVh);

        Controls.Add(textQueryPreview);

        Controls.Add(btnPull);

        Name = "Form1";

        Text = "PomReport";

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

    private DataGridViewTextBoxColumn colVh;

    private DataGridViewTextBoxColumn colVz;

    private DataGridViewTextBoxColumn colLocation;

    private TextBox _log;

}
 
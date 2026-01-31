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

        btnTestPipeline = new Button();

        textQueryPreview = new TextBox();

        _log = new TextBox();

        SuspendLayout();

        // 

        // btnPull

        // 

        btnPull.Location = new Point(12, 12);

        btnPull.Name = "btnPull";

        btnPull.Size = new Size(313, 28);

        btnPull.TabIndex = 0;

        btnPull.Text = "Pull from DB -> Save CSV";

        btnPull.UseVisualStyleBackColor = true;

        // ✅ Correct: wire the BUTTON click

        btnPull.Click += Btnpull_click;


        // 
        // btnTestPipeline
        // 
        btnTestPipeline.Location = new Point(12, 44);
        btnTestPipeline.Name = "btnTestPipeline";
        btnTestPipeline.Size = new Size(313, 28);
        btnTestPipeline.TabIndex = 1;
        btnTestPipeline.Text = "Test Pipeline (Fake Data)";
        btnTestPipeline.UseVisualStyleBackColor = true;
        btnTestPipeline.Click += btnTestPipeline_Click;

        // 

        // textQueryPreview

        // 

        textQueryPreview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        textQueryPreview.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);

        textQueryPreview.Location = new Point(12, 240);

        textQueryPreview.Multiline = true;

        textQueryPreview.Name = "textQueryPreview";

        textQueryPreview.ReadOnly = true;

        textQueryPreview.ScrollBars = ScrollBars.Both;

        textQueryPreview.Size = new Size(776, 110);

        textQueryPreview.TabIndex = 2;

        textQueryPreview.WordWrap = false;

        // 

        // _log

        // 

        _log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _log.Location = new Point(12, 360);

        _log.Multiline = true;

        _log.Name = "_log";

        _log.ScrollBars = ScrollBars.Vertical;

        _log.Size = new Size(776, 240);

        _log.TabIndex = 3;

        _log.WordWrap = false;

        // 

        // Form1

        // 

        AutoScaleDimensions = new SizeF(7F, 15F);

        AutoScaleMode = AutoScaleMode.Font;

        ClientSize = new Size(800, 640);

        Controls.Add(_log);

        Controls.Add(textQueryPreview);

        Controls.Add(btnTestPipeline);

        Controls.Add(btnPull);

        Name = "Form1";

        Text = "PomReport";

        ResumeLayout(false);

        PerformLayout();

    }

    #endregion

    private Button btnPull;

    private Button btnTestPipeline;

    private TextBox textQueryPreview;

    private TextBox _log;

}
 
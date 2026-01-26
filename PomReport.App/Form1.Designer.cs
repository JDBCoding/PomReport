namespace PomReport.App;

partial class Form1

{

    /// <summary>

    ///  Required designer variable.

    /// </summary>

    private System.ComponentModel.IContainer components = null;

    /// <summary>

    ///  Clean up any resources being used.

    /// </summary>

    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>

    protected override void Dispose(bool disposing)

    {

        if (disposing && (components != null))

        {

            components.Dispose();

        }

        base.Dispose(disposing);

    }

    #region Windows Form Designer generated code

    /// <summary>

    ///  Required method for Designer support - do not modify

    ///  the contents of this method with the code editor.

    /// </summary>

    private void InitializeComponent()

    {

        btnPull = new Button();

        _log = new TextBox();

        textLineNumbers = new TextBox();

        textQueryPreview = new TextBox();

        SuspendLayout();

        // 

        // btnPull

        // 
        btnPull = new Button();

        btnPull.Location = new Point(420, 40);

        btnPull.Name = "btnPull";

        btnPull.Size = new Size(180, 28);

        btnPull.TabIndex = 10;

        btnPull.Text = "Pull from DB -> Save CSV";

        btnPull.UseVisualStyleBackColor = true;

        btnPull.Click += btnPull_Click;

        Controls.Add(btnPull);

        // 

        // _log

        // 

        _log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _log.Location = new Point(3, 371);

        _log.Multiline = true;

        _log.Name = "_log";

        _log.ScrollBars = ScrollBars.Vertical;

        _log.Size = new Size(776, 93);

        _log.TabIndex = 1;

        _log.WordWrap = false;

        // 

        // textLineNumbers

        // 

        textLineNumbers.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        textLineNumbers.Location = new Point(12, 65);

        textLineNumbers.Multiline = true;

        textLineNumbers.Name = "textLineNumbers";

        textLineNumbers.ScrollBars = ScrollBars.Vertical;

        textLineNumbers.Size = new Size(313, 121);

        textLineNumbers.TabIndex = 2;

        textLineNumbers.WordWrap = false;

        // 

        // textQueryPreview

        // 

        textQueryPreview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        textQueryPreview.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);

        textQueryPreview.Location = new Point(12, 241);

        textQueryPreview.Multiline = true;

        textQueryPreview.Name = "textQueryPreview";

        textQueryPreview.ReadOnly = true;

        textQueryPreview.ScrollBars = ScrollBars.Both;

        textQueryPreview.Size = new Size(313, 84);

        textQueryPreview.TabIndex = 3;

        // 

        // Form1

        // 

        AutoScaleDimensions = new SizeF(7F, 15F);

        AutoScaleMode = AutoScaleMode.Font;

        AutoSize = true;

        ClientSize = new Size(800, 714);

        Controls.Add(textQueryPreview);

        Controls.Add(textLineNumbers);

        Controls.Add(_log);

        Controls.Add(btnPull);

        Name = "Form1";

        Text = "Form1";

        ResumeLayout(false);

        PerformLayout();

    }

    #endregion

    private Button btnPull;

    private TextBox _log;

    private TextBox textLineNumbers;

    private TextBox textQueryPreview;

}
 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PomReport.App;

public sealed record NcrCategorizationItem(
    string NcrId,
    string LineNumber,
    string WorkOrder,
    string Summary);

/// <summary>
/// One-time dialog to categorize new NCR one-off jobs.
/// Users choose a category (BOOM/CDS/FLTSQWKS/MISC); selections are persisted by the caller.
/// </summary>
public sealed class NcrCategorizationForm : Form
{
    public static readonly string[] AllowedCategories = ["BOOM", "CDS", "FLTSQWKS", "MISC", "LOST TOOL"];

    private readonly BindingList<RowModel> _rows;
    private readonly DataGridView _grid;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    public Dictionary<string, string> Results { get; } = new(StringComparer.OrdinalIgnoreCase);

    public NcrCategorizationForm(IReadOnlyList<NcrCategorizationItem> items)
    {
        Text = "Categorize new NCR jobs";
        StartPosition = FormStartPosition.CenterParent;
        Width = 980;
        Height = 640;
        MinimizeBox = false;
        MaximizeBox = true;

        var help = new Label
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(12, 10, 12, 6),
            Text = "These NCR one-off jobs are new. Choose a category for each (BOOM / CDS / FLTSQWKS / MISC). " +
                   "This is a one-time prompt — your choices will be remembered." 
        };

        _rows = new BindingList<RowModel>(items
            .OrderBy(i => i.NcrId, StringComparer.OrdinalIgnoreCase)
            .Select(i => new RowModel
            {
                NcrId = i.NcrId,
                LineNumber = i.LineNumber,
                WorkOrder = i.WorkOrder,
                Summary = i.Summary,
                Category = "MISC" // sensible default
            })
            .ToList());

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(RowModel.NcrId),
            HeaderText = "NCR",
            ReadOnly = true,
            FillWeight = 12
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(RowModel.LineNumber),
            HeaderText = "Line",
            ReadOnly = true,
            FillWeight = 8
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(RowModel.WorkOrder),
            HeaderText = "Work Order",
            ReadOnly = true,
            FillWeight = 12
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(RowModel.Summary),
            HeaderText = "Summary",
            ReadOnly = true,
            FillWeight = 50
        });

        var catCol = new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(RowModel.Category),
            HeaderText = "Category",
            DataSource = AllowedCategories.ToList(),
            FlatStyle = FlatStyle.Flat,
            FillWeight = 18
        };
        _grid.Columns.Add(catCol);

        _grid.DataSource = _rows;

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(12, 8, 12, 8) };
        _btnOk = new Button { Text = "Save & Continue", Width = 160, Height = 30, Anchor = AnchorStyles.Right | AnchorStyles.Top };
        _btnCancel = new Button { Text = "Cancel", Width = 100, Height = 30, Anchor = AnchorStyles.Right | AnchorStyles.Top };

        _btnOk.Left = bottom.Width - _btnCancel.Width - _btnOk.Width - 20;
        _btnCancel.Left = bottom.Width - _btnCancel.Width - 10;

        bottom.Resize += (_, __) =>
        {
            _btnCancel.Left = bottom.ClientSize.Width - _btnCancel.Width;
            _btnOk.Left = _btnCancel.Left - _btnOk.Width - 10;
        };

        _btnOk.Click += (_, __) =>
        {
            Results.Clear();
            foreach (var r in _rows)
            {
                var cat = (r.Category ?? "MISC").Trim();
                if (string.IsNullOrWhiteSpace(cat)) cat = "MISC";
                Results[r.NcrId] = cat;
            }

            DialogResult = DialogResult.OK;
            Close();
        };

        _btnCancel.Click += (_, __) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        bottom.Controls.Add(_btnOk);
        bottom.Controls.Add(_btnCancel);

        Controls.Add(_grid);
        Controls.Add(bottom);
        Controls.Add(help);
    }

    private sealed class RowModel
    {
        public string NcrId { get; set; } = "";
        public string LineNumber { get; set; } = "";
        public string WorkOrder { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Category { get; set; } = "MISC";
    }
}

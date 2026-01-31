namespace PomReport.Core.Models;

public sealed class CleanDataModel
{
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int SellCount { get; set; }

    public List<GroupBlock> Groups { get; set; } = new();

    public sealed class GroupBlock
    {
        public string LN { get; set; } = "";                  // "LN1348"
        public string Heading { get; set; } = "";             // "LN 1348 / VH110 / VZ475 - Stall 212"
        public string Class { get; set; } = "";               // CLASS/UNCLASS

        public List<CategoryBlock> Categories { get; set; } = new();
    }

    public sealed class CategoryBlock
    {
        public string Name { get; set; } = "";                // LAIRCM, CDS...
        public List<Row> Rows { get; set; } = new();
    }

    public sealed class Row
    {
        public string LN { get; set; } = "";
        public string OrderNumber { get; set; } = "";
        public string JobNumber { get; set; } = "";
        public string Description { get; set; } = "";
        public string Notes { get; set; } = "";
        public string PomComments { get; set; } = "";

        // Flags for rendering
        public bool IsNew { get; set; }
        public bool IsCommentChanged { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsSold { get; set; }

        public int SortOrder { get; set; } = 9999;
    }
}

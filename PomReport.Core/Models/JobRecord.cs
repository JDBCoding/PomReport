namespace PomReportCore.Models;

public sealed record JobRecord(
    string LineNumber,      // e.g., "VH110" or "VZ475"
    string JobNumber,       // e.g., "842-349501_REM_LOG"
    string OrderNumber,     // e.g., "12345678"
    string Description,
    string Notes,
    string PomComments)
{
    public string Key => $"{LineNumber.Trim()}|{OrderNumber.Trim()}";

    public static string NormalizeComment(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var t = s.Trim();
        if (t == "-" || t.Equals("none", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        return t;
    }

    public JobRecord Normalized() => this with
    {
        LineNumber = LineNumber?.Trim() ?? string.Empty,
        JobNumber = JobNumber?.Trim() ?? string.Empty,
        OrderNumber = OrderNumber?.Trim() ?? string.Empty,
        Description = Description?.Trim() ?? string.Empty,
        Notes = Notes?.Trim() ?? string.Empty,
        PomComments = NormalizeComment(PomComments)
    };
}

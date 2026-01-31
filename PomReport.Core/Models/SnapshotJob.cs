namespace PomReport.Core.Models;
public sealed record SnapshotJob(
   string LineNumber,
   string JobNumber,
   string OrderNumber,
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
   public SnapshotJob Normalized() => this with
   {
       LineNumber = LineNumber?.Trim() ?? "",
       JobNumber = JobNumber?.Trim() ?? "",
       OrderNumber = OrderNumber?.Trim() ?? "",
       Description = Description?.Trim() ?? "",
       Notes = Notes?.Trim() ?? "",
       PomComments = NormalizeComment(PomComments)
   };
}
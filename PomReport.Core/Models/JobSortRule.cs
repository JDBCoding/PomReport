namespace PomReportCore.Models;

public sealed record JobSortRule(
    string JobNumber,
    string Category,    // LAIRCM, CDS, Boom, etc.
    int SortOrder,      // 1..n (smaller first)
    bool SellTracked);

namespace PomReportCore.Models;

public sealed class CompareResult
{
    public List<JobRecord> NewJobs { get; } = new();
    public List<JobRecord> CompletedJobs { get; } = new();
    public List<JobRecord> CommentChangedJobs { get; } = new();
    public List<JobRecord> SoldJobs { get; } = new();

    // Jobs that exist but lack JobSort mapping
    public HashSet<string> UncategorizedJobNumbers { get; } = new(StringComparer.OrdinalIgnoreCase);
}

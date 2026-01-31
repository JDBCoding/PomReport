using PomReport.Core.Models;

namespace PomReport.Core.Models;

public sealed class CompareResult
{
    public List<SnapshotJob> NewJobs { get; } = new();
    public List<SnapshotJob> CompletedJobs { get; } = new();
    public List<SnapshotJob> CommentChangedJobs { get; } = new();
    public List<SnapshotJob> SoldJobs { get; } = new();

    // Jobs that exist but lack JobSort mapping
    public HashSet<string> UncategorizedJobNumbers { get; } = new(StringComparer.OrdinalIgnoreCase);
}

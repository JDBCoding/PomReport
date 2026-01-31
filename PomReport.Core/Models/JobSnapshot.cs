namespace PomReportCore.Models;

public sealed class JobSnapshot
{
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }   // optional: user/team lead name
    public string? Source { get; set; }      // optional: "SQL", "Test", etc.
    public List<JobRecord> Jobs { get; set; } = new();

    public JobSnapshot Normalized()
    {
        Jobs = Jobs.Select(j => j.Normalized()).ToList();
        return this;
    }
}

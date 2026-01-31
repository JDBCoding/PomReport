using PomReportCore.Models;

namespace PomReportCore.Services;

public static class FakeDataFactory
{
    public static JobSnapshot BaselineSnapshot()
    {
        return new JobSnapshot
        {
            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-5),
            Source = "Test",
            Jobs = new()
            {
                new("VH110","842-PREFLIGHT-STC-006","12727994","PREFLIGHT INSPECTION KC-46","842-PREFLIGHT-STC-006","Preflight ~80% Complete"),
                new("VH110","842-PREFLTLMI-STC-006","12734755","LAST MINUTE INSPECTION KC-46","LAST MINUTE INSPECTION KC-46",""),
                new("VH111","842-PREFLIGHT-STC-001","12335491","PREFLIGHT INSPECTION KC-46","842-PREFLIGHT-STC-001","Preflight 98% Complete"),
                new("VH110","842-349501_REM_LOG","12123560","UNPLANNED REMOVAL LOG","UNPLANNED REMOVAL LOG","Removed duct clamp - awaiting part"),
            }
        }.Normalized();
    }

    public static JobSnapshot CurrentSnapshot()
    {
        return new JobSnapshot
        {
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Source = "Test",
            Jobs = new()
            {
                // Still present, comment updated
                new("VH110","842-349501_REM_LOG","12123560","UNPLANNED REMOVAL LOG","UNPLANNED REMOVAL LOG","Removed duct clamp - part in route, install tomorrow"),

                // New job (uncategorized on purpose)
                new("VH110","842-NEW-TEST-JOB-001","12999999","NEW TEST JOB","NEW TEST JOB","Started; needs categorization"),
            }
        }.Normalized();
    }

    public static List<LineStatusEntry> LineStatus() => new()
    {
        new(1348,"VH110","VZ475","212","CLASS"),
        new(1352,"VH111","VZ421","213","CLASS"),
        new(1350,"VH421","VZ110","202","UNCLASS"),
        new(1354,"VH112","VZ107","216","UNCLASS"),
        new(1357,"VH113","VZ108","F1","UNCLASS"),
    };

    public static List<JobSortRule> JobSortRules() => new()
    {
        // Sell-tracked examples
        new("842-PREFLIGHT-STC-006","Flight",280,true),
        new("842-PREFLTLMI-STC-006","Flight",282,true),
        new("842-PREFLIGHT-STC-001","Flight",250,true),

        // Normal job
        new("842-349501_REM_LOG","Boom",84,false),
    };
}

using PomReport.Core.Models;

namespace PomReport.Core.Services;

public sealed class CompareEngine
{
    public CompareResult Compare(
        JobSnapshot current,
        JobSnapshot? baseline,
        IReadOnlyDictionary<string, JobSortRule> jobSortMap)
    {
        var result = new CompareResult();

        var currentJobs = (current.Jobs ?? new()).Select(j => j.Normalized()).ToList();
        var baseJobs = (baseline?.Jobs ?? new()).Select(j => j.Normalized()).ToList();

        var curByKey = currentJobs.ToDictionary(j => j.Key, j => j, StringComparer.OrdinalIgnoreCase);
        var baseByKey = baseJobs.ToDictionary(j => j.Key, j => j, StringComparer.OrdinalIgnoreCase);

        var curKeys = new HashSet<string>(curByKey.Keys, StringComparer.OrdinalIgnoreCase);
        var baseKeys = new HashSet<string>(baseByKey.Keys, StringComparer.OrdinalIgnoreCase);

        // NEW
        foreach (var k in curKeys.Except(baseKeys))
            result.NewJobs.Add(curByKey[k]);

        // COMPLETED
        foreach (var k in baseKeys.Except(curKeys))
            result.CompletedJobs.Add(baseByKey[k]);

        // COMMENT CHANGED (POM only)
        foreach (var k in curKeys.Intersect(baseKeys))
        {
            var cur = curByKey[k];
            var old = baseByKey[k];
            if (!string.Equals(cur.PomComments, old.PomComments, StringComparison.Ordinal))
                result.CommentChangedJobs.Add(cur);
        }

        // SOLD = completed + SellTracked
        foreach (var completed in result.CompletedJobs)
        {
            if (jobSortMap.TryGetValue(completed.JobNumber, out var rule) && rule.SellTracked)
                result.SoldJobs.Add(completed);
        }

        // Uncategorized job types
        foreach (var j in currentJobs)
            if (!jobSortMap.ContainsKey(j.JobNumber))
                result.UncategorizedJobNumbers.Add(j.JobNumber);

        return result;
    }
}

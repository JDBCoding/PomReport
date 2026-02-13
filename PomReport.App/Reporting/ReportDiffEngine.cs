using System;
using System.Collections.Generic;
using System.Linq;
using PomReport.Core.Core.Models;

namespace PomReport.App.Reporting;

/// <summary>
/// Diff rules for reporting:
/// - Key: LINENUMBER|WORKORDER
/// - Added: present in current, not in previous
/// - Sold: present in previous, not in current
/// - Updated: JobComments changed ONLY (JobNotes is ignored)
/// </summary>
public static class ReportDiffEngine
{
    public static ReportDiffResult Diff(
        IReadOnlyList<JobRecord> previous,
        IReadOnlyList<JobRecord> current)
    {
        previous ??= Array.Empty<JobRecord>();
        current ??= Array.Empty<JobRecord>();

        var prevByKey = previous
            .GroupBy(Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(Key, j => j, StringComparer.OrdinalIgnoreCase);

        var currByKey = current
            .GroupBy(Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(Key, j => j, StringComparer.OrdinalIgnoreCase);

        var added = current
            .Where(j => !prevByKey.ContainsKey(Key(j)))
            .ToList();

        var sold = previous
            .Where(j => !currByKey.ContainsKey(Key(j)))
            .ToList();

        var updated = new List<(JobRecord OldJob, JobRecord NewJob)>();
        foreach (var kv in currByKey)
        {
            if (!prevByKey.TryGetValue(kv.Key, out var oldJob))
                continue;

            var newJob = kv.Value;
            if (!CommentsEqual(oldJob.JobComments, newJob.JobComments))
                updated.Add((oldJob, newJob));
        }

        // Open jobs are current jobs.
        var open = current.ToList();

        return new ReportDiffResult(added, sold, updated, open);
    }

    internal static string Key(JobRecord j) => $"{(j.LineNumber ?? "").Trim()}|{(j.WorkOrder ?? "").Trim()}";

    private static bool CommentsEqual(string? a, string? b)
    {
        static string Norm(string? s)
        {
            var t = (s ?? string.Empty).Trim();
            if (t.Length == 0) return "-";
            return string.Join(" ", t.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        return string.Equals(Norm(a), Norm(b), StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record ReportDiffResult(
    IReadOnlyList<JobRecord> Added,
    IReadOnlyList<JobRecord> Sold,
    IReadOnlyList<(JobRecord OldJob, JobRecord NewJob)> Updated,
    IReadOnlyList<JobRecord> Open
);

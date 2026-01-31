using PomReportCore.Models;

namespace PomReportCore.Services;

public sealed class CleanDataBuilder
{
    // Canonical category order (from your DataClean sheet)
    private static readonly string[] CategoryOrder =
    [
        "LAIRCM", "CDS", "Boom", "MISC", "Software", "Flight", "Ticket"
    ];

    public CleanDataModel Build(
        JobSnapshot current,
        CompareResult compare,
        IReadOnlyDictionary<string, JobSortRule> jobSortMap,
        IReadOnlyDictionary<string, LineStatusEntry> airplaneToLineStatus)
    {
        string? ToLn(string airplane)
        {
            if (!airplaneToLineStatus.TryGetValue(airplane, out var ls)) return null;
            return $"LN{ls.Line}";
        }

        string GroupHeading(LineStatusEntry ls) =>
            $"LN {ls.Line} / {ls.VH} / {ls.VZ} - Stall {ls.Stall}";

        // flags for current rows
        var newKeys = new HashSet<string>(compare.NewJobs.Select(j => j.Key), StringComparer.OrdinalIgnoreCase);
        var changedKeys = new HashSet<string>(compare.CommentChangedJobs.Select(j => j.Key), StringComparer.OrdinalIgnoreCase);

        // build current rows
        var curRows = (current.Jobs ?? new()).Select(j => j.Normalized()).Select(j =>
        {
            var ln = ToLn(j.LineNumber) ?? "LN????";
            var category = "UNCATEGORIZED";
            var sort = 9999;

            if (jobSortMap.TryGetValue(j.JobNumber, out var rule))
            {
                category = rule.Category;
                sort = rule.SortOrder;
            }

            return new CleanDataModel.Row
            {
                LN = ln,
                OrderNumber = j.OrderNumber,
                JobNumber = j.JobNumber,
                Description = j.Description,
                Notes = j.Notes,
                PomComments = j.PomComments,
                IsNew = newKeys.Contains(j.Key),
                IsCommentChanged = changedKeys.Contains(j.Key),
                SortOrder = sort,
            };
        }).ToList();

        // completed rows from compare.CompletedJobs
        var soldKeys = new HashSet<string>(compare.SoldJobs.Select(j => j.Key), StringComparer.OrdinalIgnoreCase);

        var completedRows = compare.CompletedJobs.Select(j => j.Normalized()).Select(j =>
        {
            var ln = ToLn(j.LineNumber) ?? "LN????";
            var category = "UNCATEGORIZED";
            var sort = 9999;

            if (jobSortMap.TryGetValue(j.JobNumber, out var rule))
            {
                category = rule.Category;
                sort = rule.SortOrder;
            }

            return new CleanDataModel.Row
            {
                LN = ln,
                OrderNumber = j.OrderNumber,
                JobNumber = j.JobNumber,
                Description = j.Description,
                Notes = j.Notes,
                PomComments = j.PomComments,
                IsCompleted = true,
                IsSold = soldKeys.Contains(j.Key),
                SortOrder = sort,
            };
        }).ToList();

        var model = new CleanDataModel { SellCount = compare.SoldJobs.Count };

        var allLn = curRows.Select(r => r.LN).Concat(completedRows.Select(r => r.LN))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();

        foreach (var ln in allLn)
        {
            // find any line status entry for this LN
            LineStatusEntry? ls = airplaneToLineStatus.Values.FirstOrDefault(x => $"LN{x.Line}".Equals(ln, StringComparison.OrdinalIgnoreCase));

            var group = new CleanDataModel.GroupBlock
            {
                LN = ln,
                Heading = ls is null ? ln : GroupHeading(ls),
                Class = ls?.Class ?? ""
            };

            var cats = new List<string>(CategoryOrder);

            bool hasUncat =
                curRows.Any(r => r.LN.Equals(ln, StringComparison.OrdinalIgnoreCase) && !jobSortMap.ContainsKey(r.JobNumber)) ||
                completedRows.Any(r => r.LN.Equals(ln, StringComparison.OrdinalIgnoreCase) && !jobSortMap.ContainsKey(r.JobNumber));

            if (hasUncat) cats.Add("UNCATEGORIZED");

            foreach (var cat in cats)
            {
                bool InCat(CleanDataModel.Row r)
                {
                    if (jobSortMap.TryGetValue(r.JobNumber, out var rule))
                        return rule.Category.Equals(cat, StringComparison.OrdinalIgnoreCase);
                    return cat.Equals("UNCATEGORIZED", StringComparison.OrdinalIgnoreCase);
                }

                var cur = curRows.Where(r => r.LN.Equals(ln, StringComparison.OrdinalIgnoreCase) && InCat(r))
                    .OrderBy(r => r.SortOrder).ThenBy(r => r.OrderNumber).ToList();

                var comp = completedRows.Where(r => r.LN.Equals(ln, StringComparison.OrdinalIgnoreCase) && InCat(r))
                    .OrderBy(r => r.SortOrder).ThenBy(r => r.OrderNumber).ToList();

                if (cur.Count == 0 && comp.Count == 0) continue;

                group.Categories.Add(new CleanDataModel.CategoryBlock
                {
                    Name = cat,
                    Rows = cur.Concat(comp).ToList()
                });
            }

            model.Groups.Add(group);
        }

        return model;
    }
}

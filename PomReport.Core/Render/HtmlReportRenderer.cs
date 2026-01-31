using System;

using System.Collections.Generic;

using System.Linq;

using System.Net;

using System.Text;

using PomReport.Core.Core.Models;

namespace PomReport.Core.Render

{

    public static class HtmlReportRenderer

    {

        public static string Render(

            string title,

            DateTime generatedAt,

            IReadOnlyList<JobRecord> added,

            IReadOnlyList<JobRecord> completed,

            IReadOnlyList<(JobRecord OldJob, JobRecord NewJob)> updated,

            IReadOnlyList<JobRecord> open)

        {

            var sb = new StringBuilder();

            sb.AppendLine("<!doctype html>");

            sb.AppendLine("<html>");

            sb.AppendLine("<head>");

            sb.AppendLine("<meta charset=\"utf-8\"/>");

            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");

            sb.AppendLine($"<title>{H(title)}</title>");

            sb.AppendLine(@"
<style>

    body { font-family: Arial, sans-serif; margin: 20px; }

    h1 { margin-bottom: 0; }

    .meta { color: #555; margin-top: 4px; }

    .section { margin-top: 24px; }

    .count { color:#666; font-weight: normal; }

    table { border-collapse: collapse; width: 100%; margin-top: 10px; }

    th, td { border: 1px solid #ddd; padding: 8px; vertical-align: top; }

    th { background: #f5f5f5; text-align: left; }

    .small { font-size: 12px; color:#666; }

    .mono { font-family: Consolas, monospace; font-size: 12px; }
</style>");

            sb.AppendLine("</head>");

            sb.AppendLine("<body>");

            sb.AppendLine($"<h1>{H(title)}</h1>");

            sb.AppendLine($"<div class=\"meta\">Generated: {H(generatedAt.ToString("yyyy-MM-dd HH:mm"))}</div>");

            RenderSectionJobs(sb, "Added", added);

            RenderSectionJobs(sb, "Completed", completed);

            RenderSectionUpdated(sb, "Updated", updated);

            RenderSectionJobs(sb, "Open", open);

            sb.AppendLine("</body>");

            sb.AppendLine("</html>");

            return sb.ToString();

        }

        private static void RenderSectionJobs(StringBuilder sb, string name, IReadOnlyList<JobRecord> jobs)

        {

            sb.AppendLine("<div class=\"section\">");

            sb.AppendLine($"<h2>{H(name)} <span class=\"count\">({jobs.Count})</span></h2>");

            if (jobs.Count == 0)

            {

                sb.AppendLine("<div class=\"small\">None</div>");

                sb.AppendLine("</div>");

                return;

            }

            sb.AppendLine("<table>");

            sb.AppendLine("<thead><tr>");

            sb.AppendLine("<th>Line</th>");

            sb.AppendLine("<th>WorkOrder</th>");

            sb.AppendLine("<th>JobKit</th>");

            sb.AppendLine("<th>Description</th>");

            sb.AppendLine("<th>DailyPlan</th>");

            sb.AppendLine("<th>HeldFor</th>");

            sb.AppendLine("<th>Planned</th>");

            sb.AppendLine("<th>Actual</th>");

            sb.AppendLine("<th>Technicians</th>");

            sb.AppendLine("<th>Category</th>");

            sb.AppendLine("<th>Location</th>");

            sb.AppendLine("<th>Notes</th>");

            sb.AppendLine("<th>Comments</th>");

            sb.AppendLine("</tr></thead>");

            sb.AppendLine("<tbody>");

            foreach (var j in jobs)

            {

                sb.AppendLine("<tr>");

                sb.AppendLine($"<td class=\"mono\">{H(j.LineNumber)}</td>");

                sb.AppendLine($"<td class=\"mono\">{H(j.WorkOrder)}</td>");

                sb.AppendLine($"<td class=\"mono\">{H(j.JobKit)}</td>");

                sb.AppendLine($"<td>{H(j.JobKitDescription)}</td>");

                sb.AppendLine($"<td>{H(j.DailyPlan)}</td>");

                sb.AppendLine($"<td>{H(j.HeldFor)}</td>");

                sb.AppendLine($"<td class=\"mono\">{H(j.PlannedHours.ToString())}</td>");

                sb.AppendLine($"<td class=\"mono\">{H(j.ActualHours.ToString())}</td>");

                sb.AppendLine($"<td>{H(j.Technicians)}</td>");

                sb.AppendLine($"<td>{H(j.Category)}</td>");

                sb.AppendLine($"<td>{H(j.Location)}</td>");

                sb.AppendLine($"<td>{H(j.JobNotes)}</td>");

                sb.AppendLine($"<td>{H(j.JobComments)}</td>");

                sb.AppendLine("</tr>");

            }

            sb.AppendLine("</tbody>");

            sb.AppendLine("</table>");

            sb.AppendLine("</div>");

        }

        private static void RenderSectionUpdated(StringBuilder sb, string name, IReadOnlyList<(JobRecord OldJob, JobRecord NewJob)> updated)

        {

            sb.AppendLine("<div class=\"section\">");

            sb.AppendLine($"<h2>{H(name)} <span class=\"count\">({updated.Count})</span></h2>");

            if (updated.Count == 0)

            {

                sb.AppendLine("<div class=\"small\">None</div>");

                sb.AppendLine("</div>");

                return;

            }

            sb.AppendLine("<table>");

            sb.AppendLine("<thead><tr>");

            sb.AppendLine("<th>Key</th>");

            sb.AppendLine("<th>What changed</th>");

            sb.AppendLine("</tr></thead>");

            sb.AppendLine("<tbody>");

            foreach (var (oldJ, newJ) in updated)

            {

                var key = $"{newJ.LineNumber} | {newJ.WorkOrder}";

                var changes = DescribeChanges(oldJ, newJ);

                sb.AppendLine("<tr>");

                sb.AppendLine($"<td class=\"mono\">{H(key)}</td>");

                sb.AppendLine($"<td>{H(changes)}</td>");

                sb.AppendLine("</tr>");

            }

            sb.AppendLine("</tbody>");

            sb.AppendLine("</table>");

            sb.AppendLine("</div>");

        }

        private static string DescribeChanges(JobRecord a, JobRecord b)

        {

            var parts = new List<string>();

            void AddIfChanged(string label, string oldVal, string newVal)

            {

                oldVal ??= "";

                newVal ??= "";

                if (!string.Equals(oldVal, newVal, StringComparison.Ordinal))

                    parts.Add($"{label}: \"{oldVal}\" -> \"{newVal}\"");

            }

            AddIfChanged("DailyPlan", a.DailyPlan, b.DailyPlan);

            AddIfChanged("HeldFor", a.HeldFor, b.HeldFor);

            AddIfChanged("Technicians", a.Technicians, b.Technicians);

            AddIfChanged("JobNotes", a.JobNotes, b.JobNotes);

            AddIfChanged("JobComments", a.JobComments, b.JobComments);

            AddIfChanged("Category", a.Category, b.Category);

            AddIfChanged("Location", a.Location, b.Location);

            if (a.PlannedHours != b.PlannedHours) parts.Add($"PlannedHours: {a.PlannedHours} -> {b.PlannedHours}");

            if (a.ActualHours != b.ActualHours) parts.Add($"ActualHours: {a.ActualHours} -> {b.ActualHours}");

            return parts.Count == 0 ? "Unknown change" : string.Join(" | ", parts);

        }

        private static string H(string? s) => WebUtility.HtmlEncode(s ?? "");

    }

}
 
using System;

using System.Collections.Generic;

using System.Linq;

using System.Net;

using System.Text;

using PomReport.Config.Models;

using PomReport.Core.Core.Models;

namespace PomReport.App.Reporting;

/// <summary>

/// Email-safe (inline styles) HTML renderer for the PomReport preview.

/// Pair-first layout with "WHAT CHANGED" at the top.

/// </summary>

public static class HtmlReportRenderer

{

    public static string Render(

        string shopName,

        string exportDir,

        ReportDiffResult diff,

        IReadOnlyList<AirplanePair> airplanePairs)

    {

        var sb = new StringBuilder(64_000);

        var now = DateTime.Now;

        var baselineMissing = !System.IO.File.Exists(System.IO.Path.Combine(exportDir, "olddata.csv"));

        var pairs = (airplanePairs ?? Array.Empty<AirplanePair>()).ToList();

        var pairForLine = BuildPairLookup(pairs);

        var addedByPair = GroupByPair(diff.Added, pairForLine);

        var soldByPair = GroupByPair(diff.Sold, pairForLine);

        var updatedByPair = GroupUpdatedByPair(diff.Updated, pairForLine);

        var openByPair = GroupByPair(diff.Open, pairForLine);

        sb.AppendLine("<!doctype html>");

        sb.AppendLine("<html><head><meta charset='utf-8'></head>");

        sb.AppendLine("<body style='font-family:Segoe UI,Arial,sans-serif;font-size:12px;color:#000;'>");

        sb.AppendLine("");

        sb.AppendLine($"<div style='border:2px solid #000;background:#fff3a6;padding:10px;font-weight:700;font-size:16px;'>POM REPORT - {H(shopName)}</div>");

        sb.AppendLine($"<div style='margin:8px 0 14px 0;'><b>GENERATED:</b> {H(now.ToString("yyyy-MM-dd HH:mm"))}</div>");

        if (baselineMissing)

        {

            sb.AppendLine(

                "<div style='border:1px solid #000;background:#ffe6e6;padding:8px;margin:0 0 14px 0;'>" +

                "<b>NOTE:</b> OLDDATA.CSV WAS NOT FOUND. THIS RUN TREATS ALL CURRENT JOBS AS NEW. " +

                "AFTER THIS PREVIEW, OLDDATA WILL BE SEEDED FOR THE NEXT REPORT." +

                "</div>");

        }

        sb.AppendLine("<div style='border:2px solid #000;background:#d9edf7;padding:8px;font-weight:700;font-size:14px;margin:10px 0 6px 0;'>WHAT CHANGED (SINCE LAST SNAPSHOT)</div>");

        RenderPairSummaryTable(sb, pairs, addedByPair, soldByPair, updatedByPair, openByPair);

        RenderChangedLists(sb, pairs, addedByPair, soldByPair, updatedByPair);

        sb.AppendLine("<div style='border-top:3px solid #000;margin:18px 0 10px 0;'></div>");

        sb.AppendLine("<div style='font-weight:700;font-size:14px;margin:0 0 10px 0;'>PAIR SECTIONS</div>");

        foreach (var pair in pairs)

            RenderPairSection(sb, pair, addedByPair, soldByPair, updatedByPair, openByPair);

        RenderUnmatchedSection(sb, diff, pairForLine);

        sb.AppendLine("<div style='margin-top:18px;color:#666;'>END OF REPORT</div>");

        sb.AppendLine("</body></html>");

        return sb.ToString();

    }

    private static void RenderPairSummaryTable(

        StringBuilder sb,

        List<AirplanePair> pairs,

        Dictionary<string, List<JobRecord>> addedByPair,

        Dictionary<string, List<JobRecord>> soldByPair,

        Dictionary<string, List<(JobRecord OldJob, JobRecord NewJob)>> updatedByPair,

        Dictionary<string, List<JobRecord>> openByPair)

    {

        sb.AppendLine("<table style='border-collapse:collapse;width:100%;margin:8px 0 10px 0;'>");

        sb.AppendLine("<tr>" + Th("PAIR") + Th("ADDS") + Th("SOLD") + Th("COMMENT CHANGES") + Th("TOTAL OPEN") + "</tr>");

        foreach (var pair in pairs)

        {

            var k = PairKey(pair);

            var adds = addedByPair.TryGetValue(k, out var a) ? a.Count : 0;

            var sold = soldByPair.TryGetValue(k, out var s) ? s.Count : 0;

            var upd = updatedByPair.TryGetValue(k, out var u) ? u.Count : 0;

            var open = openByPair.TryGetValue(k, out var o) ? o.Count : 0;

            sb.AppendLine(

                "<tr>" +

                Td($"<a href='#{H(PairAnchor(pair))}' style='color:#000;text-decoration:none;'>{H(PairHeader(pair))}</a>") +

                Td(adds.ToString()) +

                Td(sold.ToString()) +

                Td(upd.ToString()) +

                Td(open.ToString()) +

                "</tr>");

        }

        sb.AppendLine("</table>");

    }

    private static void RenderChangedLists(

        StringBuilder sb,

        List<AirplanePair> pairs,

        Dictionary<string, List<JobRecord>> addedByPair,

        Dictionary<string, List<JobRecord>> soldByPair,

        Dictionary<string, List<(JobRecord OldJob, JobRecord NewJob)>> updatedByPair)

    {

        RenderChangedList(sb, "ADDS (NEW JOBS)", "#dff0d8", pairs, addedByPair, RowLine);

        RenderChangedList(sb, "SOLD (COMPLETED / REMOVED)", "#ead1f2", pairs, soldByPair, RowLine);

        sb.AppendLine("<div style='border:1px solid #000;background:#fff;padding:8px;margin:10px 0 0 0;'>");

        sb.AppendLine("<div style='font-weight:700;margin-bottom:6px;'>COMMENT CHANGES</div>");

        var any = false;

        foreach (var pair in pairs)

        {

            var k = PairKey(pair);

            if (!updatedByPair.TryGetValue(k, out var list) || list.Count == 0)

                continue;

            any = true;

            sb.AppendLine($"<div style='margin:6px 0 2px 0;font-weight:700;'><a href='#{H(PairAnchor(pair))}' style='color:#000;text-decoration:none;'>{H(PairHeader(pair))}</a></div>");

            sb.AppendLine("<ul style='margin:4px 0 8px 18px;padding:0;'>");

            foreach (var (oldJob, newJob) in list.OrderBy(x => x.NewJob.Category).ThenBy(x => x.NewJob.WorkOrder))

            {

                sb.AppendLine(

                    $"<li><b>{H(newJob.Category)}</b> → <b>WO {H(newJob.WorkOrder)}</b> → {H(newJob.JobKitDescription)}<br/>" +

                    $"<span style='color:#0b5394;font-weight:700;'>COMMENT:</span> <span style='color:#0b5394;'>{H(newJob.JobComments)}</span></li>");

            }

            sb.AppendLine("</ul>");

        }

        if (!any)

            sb.AppendLine("<div>NONE</div>");

        sb.AppendLine("</div>");

    }

    private static void RenderChangedList(

        StringBuilder sb,

        string title,

        string bg,

        List<AirplanePair> pairs,

        Dictionary<string, List<JobRecord>> byPair,

        Func<JobRecord, string> format)

    {

        sb.AppendLine($"<div style='border:1px solid #000;background:{bg};padding:8px;margin:10px 0 0 0;'>");

        sb.AppendLine($"<div style='font-weight:700;margin-bottom:6px;'>{H(title)}</div>");

        var any = false;

        foreach (var pair in pairs)

        {

            var k = PairKey(pair);

            if (!byPair.TryGetValue(k, out var list) || list.Count == 0)

                continue;

            any = true;

            sb.AppendLine($"<div style='margin:6px 0 2px 0;font-weight:700;'><a href='#{H(PairAnchor(pair))}' style='color:#000;text-decoration:none;'>{H(PairHeader(pair))}</a></div>");

            sb.AppendLine("<ul style='margin:4px 0 8px 18px;padding:0;'>");

            foreach (var j in list.OrderBy(x => x.Category).ThenBy(x => x.WorkOrder))

                sb.AppendLine($"<li>{H(format(j))}</li>");

            sb.AppendLine("</ul>");

        }

        if (!any)

            sb.AppendLine("<div>NONE</div>");

        sb.AppendLine("</div>");

    }

    private static void RenderPairSection(

        StringBuilder sb,

        AirplanePair pair,

        Dictionary<string, List<JobRecord>> addedByPair,

        Dictionary<string, List<JobRecord>> soldByPair,

        Dictionary<string, List<(JobRecord OldJob, JobRecord NewJob)>> updatedByPair,

        Dictionary<string, List<JobRecord>> openByPair)

    {

        var k = PairKey(pair);

        var adds = addedByPair.TryGetValue(k, out var a) ? a : new List<JobRecord>();

        var sold = soldByPair.TryGetValue(k, out var s) ? s : new List<JobRecord>();

        var upd = updatedByPair.TryGetValue(k, out var u) ? u : new List<(JobRecord, JobRecord)>();

        var open = openByPair.TryGetValue(k, out var o) ? o : new List<JobRecord>();

        sb.AppendLine($"<a id='{H(PairAnchor(pair))}'></a>");

        sb.AppendLine($"<div style='border:2px solid #000;background:#fff3a6;padding:8px;font-weight:700;font-size:14px;margin:16px 0 4px 0;'>{H(PairHeader(pair))}</div>");

        sb.AppendLine($"<div style='margin:0 0 8px 0;'><b>COUNTS:</b> ADDS {adds.Count} | SOLD {sold.Count} | COMMENT CHANGES {upd.Count} | TOTAL OPEN {open.Count}</div>");

        if (adds.Count > 0 || sold.Count > 0 || upd.Count > 0)

        {

            sb.AppendLine("<div style='border:1px solid #000;background:#f7f7f7;padding:8px;margin:6px 0 10px 0;'>");

            sb.AppendLine("<div style='font-weight:700;margin-bottom:6px;'>CHANGES IN THIS PAIR</div>");

            if (adds.Count > 0)

                sb.AppendLine("<div style='margin:4px 0;'><b>ADDS:</b> " + string.Join("; ", adds.OrderBy(x => x.Category).ThenBy(x => x.WorkOrder).Select(RowLine).Select(H)) + "</div>");

            if (sold.Count > 0)

                sb.AppendLine("<div style='margin:4px 0;'><b>SOLD:</b> " + string.Join("; ", sold.OrderBy(x => x.Category).ThenBy(x => x.WorkOrder).Select(RowLine).Select(H)) + "</div>");

            if (upd.Count > 0)

            {

                sb.AppendLine("<div style='margin:4px 0;'><b>COMMENT CHANGES:</b></div>");

                sb.AppendLine("<ul style='margin:4px 0 0 18px;padding:0;'>");

                foreach (var t in upd.OrderBy(x => x.Item2.Category).ThenBy(x => x.Item2.WorkOrder))
                {
                    var oldJob = t.Item1;
                    var newJob = t.Item2;

                    sb.AppendLine(

                        $"<li><b>{H(newJob.Category)}</b> → <b>WO {H(newJob.WorkOrder)}</b> → {H(newJob.JobKitDescription)}<br/>" +

                        $"<span style='color:#0b5394;font-weight:700;'>COMMENT:</span> <span style='color:#0b5394;'>{H(newJob.JobComments)}</span></li>");

                

                sb.AppendLine("</ul>");

            }

            sb.AppendLine("</div>");

        }

        var byCategory = open

            .GroupBy(j => string.IsNullOrWhiteSpace(j.Category) ? "-" : j.Category)

            .OrderBy(g => g.Key)

            .ToList();

        var addKeys = new HashSet<string>(adds.Select(ReportDiffEngine.Key), StringComparer.OrdinalIgnoreCase);

        var updKeys = new HashSet<string>(upd.Select(x => ReportDiffEngine.Key(x.Item2)), StringComparer.OrdinalIgnoreCase);

        foreach (var catGroup in byCategory)

        {

            sb.AppendLine($"<div style='margin:10px 0 4px 0;font-weight:700;font-size:13px;'>{H(catGroup.Key)}</div>");

            sb.AppendLine("<table style='border-collapse:collapse;width:100%;'>");

            sb.AppendLine("<tr>" + Th("STATUS") + Th("WORKORDER") + Th("DESCRIPTION") + Th("COMMENTS") + "</tr>");

            foreach (var j in catGroup.OrderBy(x => x.WorkOrder))

            {

                var key = ReportDiffEngine.Key(j);

                var status = "";

                var rowStyle = "";

                var commentStyle = "";

                if (addKeys.Contains(key))

                {

                    status = "[NEW]";

                    rowStyle = "background:#dff0d8;";

                }

                if (updKeys.Contains(key))

                {

                    status = status.Length > 0 ? status + " [UPDATED]" : "[UPDATED]";

                    commentStyle = "color:#0b5394;font-weight:700;";

                }

                sb.AppendLine(

                    "<tr style='" + rowStyle + "'>" +

                    Td(H(status)) +

                    Td(H(j.WorkOrder)) +

                    Td(H(j.JobKitDescription)) +

                    Td($"<span style='{commentStyle}'>{H(j.JobComments)}</span>") +

                    "</tr>");

            }

            sb.AppendLine("</table>");

        }

        }
    }

    private static void RenderUnmatchedSection(StringBuilder sb, ReportDiffResult diff, Dictionary<string, AirplanePair> pairForLine)

    {

        var unmatched = diff.Open.Where(j => !pairForLine.ContainsKey((j.LineNumber ?? "").Trim())).ToList();

        if (unmatched.Count == 0) return;

        sb.AppendLine("<div style='border:2px solid #000;background:#ffe599;padding:8px;font-weight:700;font-size:14px;margin:16px 0 4px 0;'>UNMATCHED LINE NUMBERS</div>");

        sb.AppendLine("<div style='margin:0 0 8px 0;'>THESE JOBS DID NOT MATCH ANY VH/VZ PAIR IN YOUR CONFIG. THEY ARE STILL INCLUDED HERE.</div>");

        var byLine = unmatched.GroupBy(j => j.LineNumber).OrderBy(g => g.Key);

        foreach (var g in byLine)

        {

            sb.AppendLine($"<div style='margin:10px 0 4px 0;font-weight:700;font-size:13px;'>LINE: {H(g.Key)}</div>");

            sb.AppendLine("<table style='border-collapse:collapse;width:100%;'>");

            sb.AppendLine("<tr>" + Th("STATUS") + Th("WORKORDER") + Th("DESCRIPTION") + Th("COMMENTS") + "</tr>");

            foreach (var j in g.OrderBy(x => x.WorkOrder))

                sb.AppendLine("<tr>" + Td("") + Td(H(j.WorkOrder)) + Td(H(j.JobKitDescription)) + Td(H(j.JobComments)) + "</tr>");

            sb.AppendLine("</table>");

        }

    }

    private static Dictionary<string, AirplanePair> BuildPairLookup(List<AirplanePair> pairs)

    {

        var dict = new Dictionary<string, AirplanePair>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in pairs)

        {

            var vh = (p.Vh ?? "").Trim();

            var vz = (p.Vz ?? "").Trim();

            if (vh.Length > 0 && !dict.ContainsKey(vh)) dict[vh] = p;

            if (vz.Length > 0 && !dict.ContainsKey(vz)) dict[vz] = p;

        }

        return dict;

    }

    private static Dictionary<string, List<JobRecord>> GroupByPair(IReadOnlyList<JobRecord> jobs, Dictionary<string, AirplanePair> pairForLine)

    {

        var dict = new Dictionary<string, List<JobRecord>>(StringComparer.OrdinalIgnoreCase);

        foreach (var j in jobs)

        {

            var ln = (j.LineNumber ?? "").Trim();

            if (ln.Length == 0) continue;

            if (!pairForLine.TryGetValue(ln, out var p))

                continue;

            var k = PairKey(p);

            if (!dict.TryGetValue(k, out var list))

                dict[k] = list = new List<JobRecord>();

            list.Add(j);

        }

        return dict;

    }

    private static Dictionary<string, List<(JobRecord OldJob, JobRecord NewJob)>> GroupUpdatedByPair(

        IReadOnlyList<(JobRecord OldJob, JobRecord NewJob)> updated,

        Dictionary<string, AirplanePair> pairForLine)

    {

        var dict = new Dictionary<string, List<(JobRecord, JobRecord)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in updated)

        {

            var ln = (t.NewJob.LineNumber ?? "").Trim();

            if (ln.Length == 0) continue;

            if (!pairForLine.TryGetValue(ln, out var p))

                continue;

            var k = PairKey(p);

            if (!dict.TryGetValue(k, out var list))

                dict[k] = list = new List<(JobRecord, JobRecord)>();

            list.Add(t);

        }

        return dict;

    }

    private static string PairKey(AirplanePair p)

        => $"{(p.LineNumber ?? "").Trim()}|{(p.Vh ?? "").Trim()}|{(p.Vz ?? "").Trim()}|{(p.Location ?? "").Trim()}";

    private static string PairAnchor(AirplanePair p)

        => "PAIR_" + (PairKey(p).Replace(' ', '_').Replace('|', '_'));

    private static string PairHeader(AirplanePair p)

    {

        var ln = (p.LineNumber ?? "").Trim();

        var vh = (p.Vh ?? "").Trim();

        var vz = (p.Vz ?? "").Trim();

        var loc = (p.Location ?? "").Trim();

        var parts = new List<string>();

        if (ln.Length > 0) parts.Add($"LN {ln}");

        if (vh.Length > 0) parts.Add(vh.StartsWith("VH", StringComparison.OrdinalIgnoreCase) ? vh : $"VH{vh}");

        if (vz.Length > 0) parts.Add(vz.StartsWith("VZ", StringComparison.OrdinalIgnoreCase) ? vz : $"VZ{vz}");

        var head = string.Join(" / ", parts);

        if (loc.Length > 0) head += " - " + loc;

        return head.ToUpperInvariant();

    }

    private static string RowLine(JobRecord j)

        => $"{(j.Category ?? "-").Trim()} → WO {(j.WorkOrder ?? "").Trim()} → {(j.JobKitDescription ?? "").Trim()}";

    private static string Th(string text)

        => $"<th style='border:1px solid #000;background:#333;color:#fff;padding:6px;text-align:left;'>{H(text)}</th>";

    private static string Td(string html)

        => $"<td style='border:1px solid #000;padding:6px;vertical-align:top;'>{html}</td>";

    private static string H(string? s) => WebUtility.HtmlEncode((s ?? string.Empty).ToUpperInvariant());

}
 
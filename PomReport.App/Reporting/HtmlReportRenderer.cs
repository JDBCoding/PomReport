using System;

using System.Collections.Generic;

using System.Linq;

using System.Net;

using System.Text;

using PomReport.Config.Models;

using PomReport.Core.Core.Models;

namespace PomReport.App.Reporting {

    /// <summary>

    /// PDF-like, email-safe HTML renderer:

    /// - Pair-first

    /// - Simple text blocks (NO tables per category)

    /// - Changes at top (by pair) + still visible within the pair via color/tags

    /// - Comment changed = blue comment text

    /// - New = green job line

    /// - Sold listed in Changes (not repeated inside pair details to reduce clutter)

    /// - All output uppercased

    /// </summary>

    public static class HtmlReportRenderer {

        public static string Render(

            string shopName,

            string exportDir,

            ReportDiffResult diff,

            IReadOnlyList<AirplanePair> airplanePairs) {

            var sb = new StringBuilder(64_000);

            var now = DateTime.Now;

            var baselineMissing = !System.IO.File.Exists(System.IO.Path.Combine(exportDir, "olddata.csv"));

            var pairs = (airplanePairs ?? Array.Empty<AirplanePair>()).ToList();

            var pairForLine = BuildPairLookup(pairs);

            // Group diff buckets by pair

            var addedByPair = GroupByPair(diff.Added, pairForLine);

            var soldByPair = GroupByPair(diff.Sold, pairForLine);

            var updatedByPair = GroupUpdatedByPair(diff.Updated, pairForLine);

            var openByPair = GroupByPair(diff.Open, pairForLine);

            // Convenience sets for styling inside detail sections

            var addedKeys = new HashSet<string>(diff.Added.Select(ReportDiffEngine.Key), StringComparer.OrdinalIgnoreCase);

            var updatedKeys = new HashSet<string>(diff.Updated.Select(t => ReportDiffEngine.Key(t.Item2)), StringComparer.OrdinalIgnoreCase);

            // ===== HTML HEAD =====

            sb.AppendLine("<!doctype html>");

            sb.AppendLine("<html><head><meta charset='utf-8'></head>");

            sb.AppendLine("<body style='font-family:Segoe UI,Arial,sans-serif;font-size:12px;color:#000;margin:14px;'>");

            // ===== HEADER (PDF-like) =====

            sb.AppendLine($"<div style='font-weight:800;font-size:18px;margin:0 0 4px 0;'>{H(shopName)} POM REPORT</div>");

            sb.AppendLine($"<div style='margin:0 0 8px 0;'><b>GENERATED:</b> {H(now.ToString("yyyy-MM-dd HH:mm"))}</div>");

            sb.AppendLine(

                "<div style='border:1px solid #000;background:#f7f7f7;padding:8px;margin:0 0 10px 0;'>" +

                "<b>LEGEND:</b> " +

                "<span style='color:#2e7d32;font-weight:700;'>NEW ADDS</span> | " +

                "<span style='color:#6a1b9a;font-weight:700;'>SOLD</span> | " +

                "<span style='color:#0b5394;font-weight:700;'>COMMENT UPDATED</span>" +

                "</div>");

            if (baselineMissing) {

                sb.AppendLine(

                    "<div style='border:1px solid #000;background:#ffe6e6;padding:8px;margin:0 0 10px 0;'>" +

                    "<b>NOTE:</b> OLDDATA.CSV WAS NOT FOUND. THIS RUN TREATS ALL CURRENT JOBS AS NEW. " +

                    "AFTER THIS PREVIEW, OLDDATA WILL BE SEEDED FOR THE NEXT REPORT." +

                    "</div>");

            }

            // ===== CHANGES AT TOP (BY PAIR ONLY, SIMPLE) =====

            sb.AppendLine("<div style='font-weight:800;font-size:14px;margin:14px 0 6px 0;border-top:2px solid #000;padding-top:10px;'>UPDATES</div>");

            var anyChange = false;

            foreach (var pair in pairs) {

                var k = PairKey(pair);

                var adds = addedByPair.TryGetValue(k, out var a) ? a : new List<JobRecord>();

                var sold = soldByPair.TryGetValue(k, out var s) ? s : new List<JobRecord>();

                var upd = updatedByPair.TryGetValue(k, out var u) ? u : new List<(JobRecord, JobRecord)>();

                if (adds.Count == 0 && sold.Count == 0 && upd.Count == 0)

                    continue;

                anyChange = true;

                sb.AppendLine($"<div style='margin:10px 0 4px 0;font-weight:800;'><a href='#{H(PairAnchor(pair))}' style='color:#000;text-decoration:none;'>{H(PairHeader(pair))}</a></div>");

                sb.AppendLine($"<div style='margin:0 0 6px 0;'><b>ADDS</b> {adds.Count} | <b>SOLD</b> {sold.Count} | <b>COMMENT UPDATES</b> {upd.Count}</div>");

                if (adds.Count > 0) {

                    sb.AppendLine("<div style='margin:2px 0 6px 0;'><b>ADDS:</b></div>");

                    sb.AppendLine("<ul style='margin:2px 0 8px 18px;padding:0;'>");

                    foreach (var j in adds.OrderBy(x => x.Category).ThenBy(x => x.WorkOrder))

                        sb.AppendLine($"<li><span style='color:#2e7d32;font-weight:700;'>{H(j.WorkOrder)}:</span> {H(j.JobKitDescription)} <span style='color:#666;'>({H(j.Category)})</span></li>");

                    sb.AppendLine("</ul>");

                }

                if (sold.Count > 0) {

                    sb.AppendLine("<div style='margin:2px 0 6px 0;'><b>SOLD:</b></div>");

                    sb.AppendLine("<ul style='margin:2px 0 8px 18px;padding:0;'>");

                    foreach (var j in sold.OrderBy(x => x.Category).ThenBy(x => x.WorkOrder))

                        sb.AppendLine($"<li><span style='color:#6a1b9a;font-weight:700;'>{H(j.WorkOrder)}:</span> {H(j.JobKitDescription)} <span style='color:#666;'>({H(j.Category)})</span></li>");

                    sb.AppendLine("</ul>");

                }

                if (upd.Count > 0) {

                    sb.AppendLine("<div style='margin:2px 0 6px 0;'><b>COMMENT UPDATES:</b></div>");

                    sb.AppendLine("<ul style='margin:2px 0 8px 18px;padding:0;'>");

                    foreach (var t in upd.OrderBy(x => x.Item2.Category).ThenBy(x => x.Item2.WorkOrder)) {

                        var newJob = t.Item2;

                        sb.AppendLine($"<li><span style='color:#0b5394;font-weight:700;'>{H(newJob.WorkOrder)}:</span> {H(newJob.JobKitDescription)} <span style='color:#666;'>({H(newJob.Category)})</span><br/>" +

                                      $"<span style='color:#0b5394;'>• {H(newJob.JobComments)}</span></li>");

                    }

                    sb.AppendLine("</ul>");

                }

            }

            if (!anyChange) {

                sb.AppendLine("<div style='margin:6px 0 12px 0;'>NO CHANGES DETECTED.</div>");

            }

            // ===== DETAIL SECTIONS (PAIR-FIRST, SIMPLE) =====

            sb.AppendLine("<div style='font-weight:800;font-size:14px;margin:18px 0 6px 0;border-top:2px solid #000;padding-top:10px;'>AIRPLANES</div>");

            foreach (var pair in pairs) {

                var k = PairKey(pair);

                var open = openByPair.TryGetValue(k, out var o) ? o : new List<JobRecord>();

                var adds = addedByPair.TryGetValue(k, out var a) ? a : new List<JobRecord>();

                var sold = soldByPair.TryGetValue(k, out var s) ? s : new List<JobRecord>();

                var upd = updatedByPair.TryGetValue(k, out var u) ? u : new List<(JobRecord, JobRecord)>();

                // Pair header bar (simple)

                sb.AppendLine($"<a id='{H(PairAnchor(pair))}'></a>");

                sb.AppendLine(

                    "<div style='border:1px solid #000;background:#fff3a6;padding:8px;margin:14px 0 6px 0;font-weight:800;'>" +

                    $"{H(PairHeader(pair))}</div>");

                sb.AppendLine(

                    $"<div style='margin:0 0 10px 0;'>" +

                    $"<b>ADDS</b> {adds.Count} | <b>SOLD</b> {sold.Count} | <b>COMMENT UPDATES</b> {upd.Count} | <b>TOTAL OPEN</b> {open.Count}" +

                    $"</div>");

                if (open.Count == 0) {

                    sb.AppendLine("<div style='margin:0 0 10px 0;color:#666;'>NO OPEN JOBS.</div>");

                    continue;

                }

                // Category blocks as simple text (PDF-like)

                var byCat = open

                    .GroupBy(j => string.IsNullOrWhiteSpace(j.Category) ? "-" : j.Category)

                    .OrderBy(g => g.Key)

                    .ToList();

                foreach (var cat in byCat) {

                    sb.AppendLine($"<div style='font-weight:800;margin:10px 0 4px 0;'>{H(cat.Key)}</div>");

                    sb.AppendLine("<ul style='margin:2px 0 8px 18px;padding:0;'>");

                    foreach (var j in cat.OrderBy(x => x.WorkOrder)) {

                        var key = ReportDiffEngine.Key(j);

                        var isAdd = addedKeys.Contains(key);

                        var isUpd = updatedKeys.Contains(key);

                        var jobLineColor = isAdd ? "color:#2e7d32;font-weight:700;" : "";

                        var commentColor = isUpd ? "color:#0b5394;font-weight:700;" : "color:#000;";

                        // job line: "WO ####: DESCRIPTION"

                        sb.AppendLine($"<li><span style='{jobLineColor}'>{H(j.WorkOrder)}:</span> {H(j.JobKitDescription)}</li>");

                        // comment line (only if present). If updated, it's blue.

                        if (!string.IsNullOrWhiteSpace(j.JobComments) && j.JobComments.Trim() != "-") {

                            sb.AppendLine($"<div style='margin:2px 0 6px 18px;{commentColor}'>• {H(j.JobComments)}</div>");

                        }

                    }

                    sb.AppendLine("</ul>");

                }

            }

            // ===== UNMATCHED (if any) =====

            RenderUnmatchedSection(sb, diff, pairForLine);

            sb.AppendLine("<div style='margin-top:18px;color:#666;'>END OF REPORT</div>");

            sb.AppendLine("</body></html>");

            return sb.ToString();

        }

        // -------- Helpers --------

        private static Dictionary<string, AirplanePair> BuildPairLookup(List<AirplanePair> pairs) {

            var dict = new Dictionary<string, AirplanePair>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in pairs) {

                var vh = (p.Vh ?? "").Trim();

                var vz = (p.Vz ?? "").Trim();

                if (vh.Length > 0 && !dict.ContainsKey(vh)) dict[vh] = p;

                if (vz.Length > 0 && !dict.ContainsKey(vz)) dict[vz] = p;

            }

            return dict;

        }

        private static Dictionary<string, List<JobRecord>> GroupByPair(IReadOnlyList<JobRecord> jobs, Dictionary<string, AirplanePair> pairForLine) {

            var dict = new Dictionary<string, List<JobRecord>>(StringComparer.OrdinalIgnoreCase);

            foreach (var j in jobs) {

                var ln = (j.LineNumber ?? "").Trim();

                if (ln.Length == 0) continue;

                if (!pairForLine.TryGetValue(ln, out var p))

                    continue;

                var k = PairKey(p);

                if (!dict.TryGetValue(k, out var list))

                    dict[k] = list = new List<JobRecord>();

                dict[k].Add(j);

            }

            return dict;

        }

        private static Dictionary<string, List<(JobRecord OldJob, JobRecord NewJob)>> GroupUpdatedByPair(

            IReadOnlyList<(JobRecord OldJob, JobRecord NewJob)> updated,

            Dictionary<string, AirplanePair> pairForLine) {

            var dict = new Dictionary<string, List<(JobRecord, JobRecord)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in updated) {

                var ln = (t.NewJob.LineNumber ?? "").Trim();

                if (ln.Length == 0) continue;

                if (!pairForLine.TryGetValue(ln, out var p))

                    continue;

                var k = PairKey(p);

                if (!dict.TryGetValue(k, out var list))

                    dict[k] = list = new List<(JobRecord, JobRecord)>();

                dict[k].Add((t.OldJob, t.NewJob));

            }

            // return with named tuple type compatibility

            return dict.ToDictionary(

                kvp => kvp.Key,

                kvp => kvp.Value.Select(x => (OldJob: x.Item1, NewJob: x.Item2)).ToList(),

                StringComparer.OrdinalIgnoreCase);

        }

        private static string PairKey(AirplanePair p)

            => $"{(p.LineNumber ?? "").Trim()}|{(p.Vh ?? "").Trim()}|{(p.Vz ?? "").Trim()}|{(p.Location ?? "").Trim()}";

        private static string PairAnchor(AirplanePair p)

            => "PAIR_" + PairKey(p).Replace(' ', '_').Replace('|', '_');

        private static string PairHeader(AirplanePair p) {

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

            return head;

        }

        private static void RenderUnmatchedSection(StringBuilder sb, ReportDiffResult diff, Dictionary<string, AirplanePair> pairForLine) {

            var unmatched = diff.Open.Where(j => !pairForLine.ContainsKey((j.LineNumber ?? "").Trim())).ToList();

            if (unmatched.Count == 0) return;

            sb.AppendLine("<div style='border:1px solid #000;background:#ffe599;padding:8px;font-weight:800;margin:16px 0 6px 0;'>UNMATCHED LINE NUMBERS</div>");

            sb.AppendLine("<div style='margin:0 0 10px 0;color:#444;'>THESE JOBS DID NOT MATCH ANY VH/VZ PAIR IN YOUR CONFIG.</div>");

            foreach (var g in unmatched.GroupBy(j => j.LineNumber).OrderBy(x => x.Key)) {

                sb.AppendLine($"<div style='font-weight:800;margin:10px 0 4px 0;'>LINE: {H(g.Key)}</div>");

                sb.AppendLine("<ul style='margin:2px 0 8px 18px;padding:0;'>");

                foreach (var j in g.OrderBy(x => x.WorkOrder))

                    sb.AppendLine($"<li>{H(j.WorkOrder)}: {H(j.JobKitDescription)}</li>");

                sb.AppendLine("</ul>");

            }

        }

        private static string H(string? s) => WebUtility.HtmlEncode((s ?? "").ToUpperInvariant());

    }

}

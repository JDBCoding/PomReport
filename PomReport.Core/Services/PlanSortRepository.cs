using System;

using System.Collections.Generic;

using System.Globalization;

using System.IO;

using System.Linq;

using System.Text;

namespace PomReportCore.Models

{

    /// <summary>

    /// One row from the shop's Plan Sort CSV:

    /// PlanNumber,Category,Priority

    /// </summary>

    public sealed record PlanSortRule(

        string PlanNumber,

        string Category,

        int Priority

    );

}

namespace PomReportCore.Services

{

    using PomReportCore.Models;

    /// <summary>

    /// Loads the shop-provided CSV mapping PlanNumber -> Category/Priority.

    /// Keeps parsing strict on required columns, tolerant of extra columns.

    /// </summary>

    public sealed class PlanSortRepository

    {

        public string CsvPath { get; }

        public PlanSortRepository(string csvPath)

        {

            CsvPath = csvPath ?? throw new ArgumentNullException(nameof(csvPath));

        }

        public bool Exists() => File.Exists(CsvPath);

        /// <summary>

        /// Loads PlanNumber -> PlanSortRule map. If file doesn't exist, returns empty map.

        /// Duplicate PlanNumbers: last row wins.

        /// </summary>

        public IReadOnlyDictionary<string, PlanSortRule> LoadMap()

        {

            if (!File.Exists(CsvPath))

                return new Dictionary<string, PlanSortRule>(StringComparer.OrdinalIgnoreCase);

            var lines = File.ReadAllLines(CsvPath);

            // remove blank + comment lines

            var cleaned = lines

                .Select(l => (l ?? "").Trim())

                .Where(l => !string.IsNullOrWhiteSpace(l))

                .Where(l => !(l.StartsWith("#") || l.StartsWith("//")))

                .ToList();

            if (cleaned.Count == 0)

                return new Dictionary<string, PlanSortRule>(StringComparer.OrdinalIgnoreCase);

            var header = SplitCsvLine(cleaned[0]);

            var col = BuildHeaderMap(header);

            if (!col.TryGetValue("plannumber", out var iPlan))

                throw new InvalidDataException("Plan Sort CSV missing required column: PlanNumber");

            if (!col.TryGetValue("category", out var iCat))

                throw new InvalidDataException("Plan Sort CSV missing required column: Category");

            // allow "priority" or "sortorder" (shops may call it either)

            if (!col.TryGetValue("priority", out var iPri))

                col.TryGetValue("sortorder", out iPri);

            if (iPri < 0)

                throw new InvalidDataException("Plan Sort CSV missing required column: Priority (or SortOrder)");

            var map = new Dictionary<string, PlanSortRule>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < cleaned.Count; i++)

            {

                var cols = SplitCsvLine(cleaned[i]);

                if (cols.Count == 0) continue;

                var plan = Get(cols, iPlan).Trim();

                var cat = Get(cols, iCat).Trim();

                var priRaw = Get(cols, iPri).Trim();

                if (string.IsNullOrWhiteSpace(plan) || string.IsNullOrWhiteSpace(cat))

                    continue;

                if (!int.TryParse(priRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pri))

                    pri = 9999;

                map[plan] = new PlanSortRule(plan, cat, pri);

            }

            return map;

        }

        /// <summary>

        /// Writes a ready-to-fill template CSV to CsvPath.

        /// Does NOT overwrite unless overwrite=true.

        /// </summary>

        public void WriteTemplate(bool overwrite = false)

        {

            var dir = Path.GetDirectoryName(CsvPath);

            if (!string.IsNullOrWhiteSpace(dir))

                Directory.CreateDirectory(dir);

            if (File.Exists(CsvPath) && !overwrite)

                return;

            var sb = new StringBuilder();

            sb.AppendLine("PlanNumber,Category,Priority");

            sb.AppendLine("842-349501_REM_LOG,Traveler,10");

            sb.AppendLine("842-UNPLND-REM-LOG-ACC574P1,Boom,20");

            File.WriteAllText(CsvPath, sb.ToString());

        }

        // ---------------- helpers ----------------

        private static string Get(List<string> cols, int idx)

        {

            if (idx < 0) return "";

            if (idx >= cols.Count) return "";

            return cols[idx] ?? "";

        }

        private static Dictionary<string, int> BuildHeaderMap(List<string> header)

        {

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < header.Count; i++)

            {

                var key = (header[i] ?? "").Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(key)) continue;

                key = key.Replace(" ", "");

                // normalize common variants

                if (key is "plan" or "plannum" or "jobkit")

                    key = "plannumber";

                if (key is "order" or "sort" or "sortorder")

                    key = "sortorder";

                if (!map.ContainsKey(key))

                    map[key] = i;

            }

            return map;

        }

        private static List<string> SplitCsvLine(string line)

        {

            var result = new List<string>();

            if (line == null) return result;

            var sb = new StringBuilder();

            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)

            {

                var c = line[i];

                if (c == '"')

                {

                    // escaped quote ""

                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')

                    {

                        sb.Append('"');

                        i++;

                    }

                    else

                    {

                        inQuotes = !inQuotes;

                    }

                }

                else if (c == ',' && !inQuotes)

                {

                    result.Add(sb.ToString());

                    sb.Clear();

                }

                else

                {

                    sb.Append(c);

                }

            }

            result.Add(sb.ToString());

            return result;

        }

    }

}

using System;

using System.Collections.Generic;

using System.Globalization;

using System.IO;

using Microsoft.VisualBasic.FileIO;

using PomReport.Core.Models;

namespace PomReport.Data.Csv

{

    public static class CsvJobSource

    {

        public static List<JobRecord> Load(string csvPath)

        {

            if (string.IsNullOrWhiteSpace(csvPath))

                throw new ArgumentException("csvPath is blank.", nameof(csvPath));

            if (!File.Exists(csvPath))

                throw new FileNotFoundException("CSV not found.", csvPath);

            var jobs = new List<JobRecord>();

            using var parser = new TextFieldParser(csvPath);

            parser.TextFieldType = FieldType.Delimited;

            parser.SetDelimiters(",");

            parser.HasFieldsEnclosedInQuotes = true;

            parser.TrimWhiteSpace = true;

            // Header row

            if (parser.EndOfData)

                return jobs;

            var headers = parser.ReadFields() ?? Array.Empty<string>();

            var headerIndex = BuildHeaderIndex(headers);

            while (!parser.EndOfData)

            {

                var fields = parser.ReadFields();

                if (fields == null || fields.Length == 0)

                    continue;

                // Pull values using new headers first, but tolerate old ones too.

                long jobId = ParseLong(Get(fields, headerIndex, "JobId", "JobID"), 0);

                string lineNumber = Get(fields, headerIndex, "LineNumber") ?? "";

                string workOrder = Get(fields, headerIndex, "WorkOrder", "JobNumber") ?? "";

                string jobKitDescription = Get(fields, headerIndex, "JobKitDescription", "Description") ?? "";

                string jobNotes = Get(fields, headerIndex, "JobNotes", "Note") ?? "-";

                decimal plannedHours = ParseDecimal(Get(fields, headerIndex, "PlannedHours"), 0m);

                string jobComments = Get(fields, headerIndex, "JobComments", "LatestComment", "Comment") ?? "-";

                string technicians = Get(fields, headerIndex, "Technicians") ?? "-";

                string dailyPlan = Get(fields, headerIndex, "DailyPlan") ?? "-";

                decimal actualHours = ParseDecimal(Get(fields, headerIndex, "ActualHours"), 0m);

                string jobKit = Get(fields, headerIndex, "JobKit", "PartNumber") ?? "-";

                string parentWorkOrder = Get(fields, headerIndex, "ParentWorkOrder", "ParentJobNumber") ?? "-";

                string heldFor = Get(fields, headerIndex, "HeldFor") ?? "-";

                // These two may not exist in SQL output yet — keep safe defaults.

                string category = Get(fields, headerIndex, "Category") ?? "-";

                string location = Get(fields, headerIndex, "Location") ?? "-";

                // Skip totally empty lines that sneak into CSVs

                if (string.IsNullOrWhiteSpace(lineNumber) && string.IsNullOrWhiteSpace(workOrder))

                    continue;

                jobs.Add(new JobRecord(

                    jobId,

                    lineNumber,

                    workOrder,

                    jobKitDescription,

                    jobNotes,

                    plannedHours,

                    jobComments,

                    technicians,

                    dailyPlan,

                    actualHours,

                    jobKit,

                    parentWorkOrder,

                    heldFor,

                    category,

                    location

                ));

            }

            return jobs;

        }

        private static Dictionary<string, int> BuildHeaderIndex(string[] headers)

        {

            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Length; i++)

            {

                var h = (headers[i] ?? "").Trim();

                if (h.Length == 0) continue;

                // If duplicates exist, keep the first

                if (!dict.ContainsKey(h))

                    dict[h] = i;

            }

            return dict;

        }

        private static string? Get(string[] fields, Dictionary<string, int> headerIndex, params string[] names)

        {

            foreach (var name in names)

            {

                if (headerIndex.TryGetValue(name, out int idx))

                {

                    if (idx >= 0 && idx < fields.Length)

                        return fields[idx];

                }

            }

            return null;

        }

        private static long ParseLong(string? s, long fallback)

        {

            if (long.TryParse((s ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))

                return v;

            return fallback;

        }

        private static decimal ParseDecimal(string? s, decimal fallback)

        {

            if (decimal.TryParse((s ?? "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v))

                return v;

            return fallback;

        }

    }

}
 
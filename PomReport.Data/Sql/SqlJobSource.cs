using System;

using System.Collections.Generic;

using System.Data;

using System.Data.OleDb;

using System.Globalization;

using System.IO;

using System.Linq;

using System.Text;

using System.Text.RegularExpressions;

using System.Threading;

using System.Threading.Tasks;

namespace PomReport.Data.Sql

{

    public static class SqlJobSource

    {

        // Strict allow-list for VH / VZ tokens (prevents SQL injection)

        private static readonly Regex SafeToken =

            new Regex(@"^[A-Za-z0-9_-]{1,50}$", RegexOptions.Compiled);

        /// <summary>

        /// Executes the provided SQL template, replaces {LINE_NUMBER_PARAMS}

        /// with a VH/VZ IN-list, and writes the result to a CSV file.

        /// </summary>

        public static async Task<string> PullToCsvAsync(

            string connectionString,

            string sqlTemplate,

            IEnumerable<string> vhVzValues,

            string outputCsvFullPath,

            int commandTimeoutSeconds = 300,

            CancellationToken ct = default)

        {

            if (string.IsNullOrWhiteSpace(connectionString))

                throw new ArgumentException("Connection string is empty.", nameof(connectionString));

            if (string.IsNullOrWhiteSpace(sqlTemplate))

                throw new ArgumentException("SQL template is empty.", nameof(sqlTemplate));

            if (!sqlTemplate.Contains("{LINE_NUMBER_PARAMS}", StringComparison.Ordinal))

                throw new InvalidOperationException(

                    "SQL template must contain {LINE_NUMBER_PARAMS} for the IN (...) clause.");

            // Normalize + validate VH / VZ list

            var tokens = (vhVzValues ?? Enumerable.Empty<string>())

                .Select(v => (v ?? "").Trim())

                .Where(v => !string.IsNullOrWhiteSpace(v))

                .Distinct(StringComparer.OrdinalIgnoreCase)

                .ToList();

            if (tokens.Count == 0)

                throw new InvalidOperationException("No VH/VZ values provided for SQL pull.");

            foreach (var token in tokens)

            {

                if (!SafeToken.IsMatch(token))

                    throw new InvalidOperationException(

                        $"Invalid VH/VZ value '{token}'. Allowed: A-Z, a-z, 0-9, '_' and '-' (max 50 chars).");

            }

            // Build SQL IN-list:  'VH123','VZ456',...

            var inList = string.Join(", ", tokens.Select(t => $"'{t.Replace("'", "''")}'"));

            var finalSql = sqlTemplate.Replace("{LINE_NUMBER_PARAMS}", inList);

            var outDir = Path.GetDirectoryName(outputCsvFullPath);

            if (!string.IsNullOrWhiteSpace(outDir))

                Directory.CreateDirectory(outDir);

            await using var conn = new OleDbConnection(connectionString);

            await conn.OpenAsync(ct);

            await using var cmd = new OleDbCommand(finalSql, conn)

            {

                CommandTimeout = commandTimeoutSeconds

            };

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            await using var fs = new FileStream(

                outputCsvFullPath,

                FileMode.Create,

                FileAccess.Write,

                FileShare.Read);

            await using var sw = new StreamWriter(fs, new UTF8Encoding(false));

            // --- CSV HEADER ---

            for (int i = 0; i < reader.FieldCount; i++)

            {

                if (i > 0) await sw.WriteAsync(",");

                await sw.WriteAsync(EscapeCsv(reader.GetName(i)));

            }

            await sw.WriteLineAsync();

            // --- CSV ROWS ---

            while (await reader.ReadAsync(ct))

            {

                for (int i = 0; i < reader.FieldCount; i++)

                {

                    if (i > 0) await sw.WriteAsync(",");

                    await sw.WriteAsync(EscapeCsv(ToInvariantString(reader.GetValue(i))));

                }

                await sw.WriteLineAsync();

            }

            await sw.FlushAsync();

            return outputCsvFullPath;

        }

        // ------------------------------------------------------------

        // Helpers

        // ------------------------------------------------------------

        private static string ToInvariantString(object? value)

        {

            if (value == null || value == DBNull.Value)

                return "";

            return value switch

            {

                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),

                decimal d => d.ToString(CultureInfo.InvariantCulture),

                double d => d.ToString(CultureInfo.InvariantCulture),

                float f => f.ToString(CultureInfo.InvariantCulture),

                _ => value.ToString() ?? ""

            };

        }

        private static string EscapeCsv(string value)

        {

            if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))

            {

                value = value.Replace("\"", "\"\"");

                return $"\"{value}\"";

            }

            return value;

        }

    }

}
 
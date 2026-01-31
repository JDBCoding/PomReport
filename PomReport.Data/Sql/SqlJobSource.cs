using System;

using System.Collections.Generic;

using System.Globalization;

using System.IO;

using System.Linq;

using System.Text;

using System.Threading;

using System.Threading.Tasks;

using System.Data;

using System.Data.OleDb;

namespace PomReport.Data.Sql

{

    public static class SqlJobSource

    {

        // Optional default (you can ignore this and pass a connection string into PullToCsvAsync)

        public static string ConnectionString { get; set; } =

            "Server=YOURSERVER;Database=Newton_Release;Integrated Security=True;TrustServerCertificate=True;";

        public static async Task<bool> TestConnectionAsync(

            string connectionString,

            CancellationToken ct = default)

        {

            await using var conn = new OleDbConnection(connectionString);

            await conn.OpenAsync(ct);

            return conn.State == System.Data.ConnectionState.Open;

        }

        public static async Task<string> PullToCsvAsync(

            string connectionString,

            string sqlTemplate,

            IEnumerable<string> lineNumbers,

            string outputFolder,

            string filePrefix = "POM_DB_Pull",

            int commandTimeoutSeconds = 300,

            CancellationToken ct = default)

        {

            if (string.IsNullOrWhiteSpace(connectionString))

                throw new ArgumentException("Connection string is empty.", nameof(connectionString));

            if (string.IsNullOrWhiteSpace(sqlTemplate))

                throw new ArgumentException("SQL template is empty.", nameof(sqlTemplate));

            var inList = lineNumbers?

                .Select(x => x!)

                .Where(x => !string.IsNullOrWhiteSpace(x))

                .ToList();

            if (inList.Count == 0)

                throw new ArgumentException("No line numbers provided.", nameof(lineNumbers));

            // Build parameter names: @ln0,@ln1,...

            var paramNames = inList.Select((_, i) => $"@ln{i}").ToList();

            var inParams = string.Join(", ", paramNames);

            if (!sqlTemplate.Contains("{LINE_NUMBER_PARAMS}", StringComparison.Ordinal))

            {

                throw new InvalidOperationException(

                    "SQL template must contain the token {LINE_NUMBER_PARAMS} where the IN (...) list belongs.");

            }

            var sql = sqlTemplate.Replace("{LINE_NUMBER_PARAMS}", inParams);

            Directory.CreateDirectory(outputFolder);

            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            var fullPath = Path.Combine(outputFolder, fileName);

            await using var conn = new OleDbConnection(connectionString);

            await conn.OpenAsync(ct);

            await using var cmd = new OleDbCommand("SELECT @@VERSION", conn);
            var v = cmd.ExecuteScalar();

            {

                cmd.CommandTimeout = commandTimeoutSeconds;

            };

            // Add parameters safely

            for (int i = 0; i < inList.Count; i++)

            {

                // Adjust size if needed; 50 is usually plenty for VH/VZ/LineNumber values

                cmd.Parameters.Add(new OleDbParameter

                {

                    OleDbType = OleDbType.VarChar,
                    Size = 50,
                    Value = inList[i]

                });

            }

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);

            await using var sw = new StreamWriter(fs, new UTF8Encoding(false));

            // Header row

            for (int i = 0; i < reader.FieldCount; i++)

            {

                if (i > 0) await sw.WriteAsync(",");

                await sw.WriteAsync(Escape(reader.GetName(i)));

            }

            await sw.WriteLineAsync();

            // Data rows

            while (await reader.ReadAsync(ct))

            {

                for (int i = 0; i < reader.FieldCount; i++)

                {

                    if (i > 0) await sw.WriteAsync(",");

                    await sw.WriteAsync(Escape(ToInvariant(reader.GetValue(i))));

                }

                await sw.WriteLineAsync();

            }

            await sw.FlushAsync();

            return fullPath;

        }

        private static string ToInvariant(object? value)

        {

            if (value == null || value == DBNull.Value)

                return "";

            return value switch

            {

                decimal d => d.ToString(CultureInfo.InvariantCulture),

                double d => d.ToString(CultureInfo.InvariantCulture),

                float f => f.ToString(CultureInfo.InvariantCulture),

                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),

                _ => value.ToString() ?? ""

            };

        }

        private static string Escape(string s)

        {

            if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))

            {

                s = s.Replace("\"", "\"\"");

                return $"\"{s}\"";

            }

            return s;

        }

    }

}
 
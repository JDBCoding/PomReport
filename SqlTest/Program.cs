using System;

using System.Collections.Generic;

using System.Data;

using System.Data.OleDb;

using System.IO;

using System.Linq;

using System.Text.Json;

class Program

{

    static int Main(string[] args)

    {

        try

        {

            // 1) Load connection string from config.json (same as app)

            var configPath = FindConfigJsonPath();

            var connectionString = LoadConnectionString(configPath);

            // 2) Load base SQL from Copy_SQL_query.txt (this is your B1 / base query)

            var baseSqlPath = FindFileUpTree("Copy_SQL_query.txt");

            var baseSql = File.ReadAllText(baseSqlPath).TrimEnd();

            // 3) Parse flat list values (VH + VZ) from command line args

            var values = ParseFlatList(args);

            if (values.Count == 0)

            {

                PrintUsage();

                Console.WriteLine();

                Console.WriteLine("ERROR: No line numbers provided.");

                return 2;

            }

            // 4) Build Excel-style B2 string: ('VH110','VZ475',...)

            var inList = BuildExcelInList(values);

            // 5) Combine base SQL + in-list (exactly like Excel: B1 & B2)

            // Your base SQL ends with: AND jl.LineNumber IN

            // Excel appends: ('VH110',...)

            var finalSql = CombineBaseSqlAndInList(baseSql, inList);

            Console.WriteLine("===== SqlTest Runner =====");

            Console.WriteLine($"Config        : {configPath}");

            Console.WriteLine($"SQL base file : {baseSqlPath}");

            Console.WriteLine($"Items         : {values.Count}");

            Console.WriteLine();

            Console.WriteLine("----- IN LIST (Excel-style) -----");

            Console.WriteLine(inList);

            Console.WriteLine("----- END IN LIST -----");

            Console.WriteLine();

            // Helpful: show the last ~200 chars so you can confirm IN(...) is attached

            Console.WriteLine("----- FINAL SQL (tail) -----");

            var tail = finalSql.Length > 200 ? finalSql[^200..] : finalSql;

            Console.WriteLine(tail);

            Console.WriteLine("----- END FINAL SQL (tail) -----");

            Console.WriteLine();

            // 6) Execute and print results

            RunQuery(connectionString, finalSql, maxRowsToPrint: 25);

            Console.WriteLine();

            Console.WriteLine("Done.");

            return 0;

        }

        catch (Exception ex)

        {

            Console.WriteLine("FAILED");

            Console.WriteLine(ex.GetType().FullName);

            Console.WriteLine(ex.Message);

            return 1;

        }

    }

    private static void RunQuery(string connectionString, string sql, int maxRowsToPrint)

    {

        using var conn = new OleDbConnection(connectionString);

        conn.Open();

        using var cmd = new OleDbCommand(sql, conn)

        {

            CommandTimeout = 300

        };

        using var rdr = cmd.ExecuteReader(CommandBehavior.SequentialAccess);

        if (rdr == null)

        {

            Console.WriteLine("No reader returned.");

            return;

        }

        // Print columns

        Console.WriteLine($"Columns: {rdr.FieldCount}");

        for (int i = 0; i < rdr.FieldCount; i++)

            Console.WriteLine($"  [{i}] {rdr.GetName(i)}");

        Console.WriteLine();

        int row = 0;

        while (rdr.Read())

        {

            row++;

            if (row <= maxRowsToPrint)

            {

                Console.WriteLine($"--- row {row} ---");

                for (int i = 0; i < rdr.FieldCount; i++)

                {

                    var val = rdr.IsDBNull(i) ? "<NULL>" : rdr.GetValue(i);

                    Console.WriteLine($"{rdr.GetName(i)} = {val}");

                }

                Console.WriteLine();

            }

        }

        Console.WriteLine($"Total rows returned: {row}");

    }

    private static List<string> ParseFlatList(string[] args)

    {

        if (args == null || args.Length == 0)

            return new List<string>();

        // Allow either:

        //  - comma-separated in one arg: VH110,VZ475,VH111

        //  - space separated args: VH110 VZ475 VH111

        var joined = string.Join(" ", args).Trim();

        if (joined.Contains(","))

        {

            return joined

                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)

                .Where(x => !string.IsNullOrWhiteSpace(x))

                .ToList();

        }

        return joined

            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)

            .Where(x => !string.IsNullOrWhiteSpace(x))

            .ToList();

    }

    private static string BuildExcelInList(List<string> values)

    {

        // Match Excel behavior: quote every value, comma-separated, parentheses.

        // Do NOT dedupe; do NOT reorder.

        var quoted = values.Select(v => $"'{v.Replace("'", "''")}'");

        return "(" + string.Join(",", quoted) + ")";

    }

    private static string CombineBaseSqlAndInList(string baseSql, string inList)

    {

        // Your base file ends with "... IN" (with possible trailing whitespace).

        // Excel does: B1 & B2, so we do the same: baseSql + inList.

        var trimmed = baseSql.TrimEnd();

        // If it already ends with ")" we assume someone already pasted the list in

        if (trimmed.EndsWith(")", StringComparison.Ordinal))

            return trimmed;

        // If it ends with "IN" (your current case), append a space + inList

        if (trimmed.EndsWith("IN", StringComparison.OrdinalIgnoreCase))

            return trimmed + " " + inList;

        // If it ends with "IN " etc. still safe to append

        return trimmed + " " + inList;

    }

    private static string LoadConnectionString(string configPath)

    {

        var json = File.ReadAllText(configPath);

        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("connectionString", out var csProp))

            throw new InvalidOperationException("config.json is missing 'connectionString'.");

        var cs = csProp.GetString() ?? "";

        if (string.IsNullOrWhiteSpace(cs))

            throw new InvalidOperationException("config.json 'connectionString' is empty.");

        return cs;

    }

    private static string FindConfigJsonPath()

    {

        // Looks for config.json in common locations:

        // - current directory

        // - repo root

        // - PomReport.App\config.json

        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        for (int i = 0; i < 8 && dir != null; i++)

        {

            var p1 = Path.Combine(dir.FullName, "config.json");

            if (File.Exists(p1)) return p1;

            var p2 = Path.Combine(dir.FullName, "PomReport.App", "config.json");

            if (File.Exists(p2)) return p2;

            dir = dir.Parent;

        }

        throw new FileNotFoundException("Could not find config.json. Put it in repo root or PomReport.App\\config.json.");

    }

    private static string FindFileUpTree(string fileName)

    {

        // Finds Copy_SQL_query.txt from current directory upward

        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        for (int i = 0; i < 8 && dir != null; i++)

        {

            var candidate = Path.Combine(dir.FullName, fileName);

            if (File.Exists(candidate))

                return candidate;

            // handle nested repo folder name if needed

            var nested = Path.Combine(dir.FullName, "PomReport-main", fileName);

            if (File.Exists(nested))

                return nested;

            dir = dir.Parent;

        }

        throw new FileNotFoundException($"Could not find {fileName} from current directory.");

    }

    private static void PrintUsage()

    {

        Console.WriteLine("Usage:");

        Console.WriteLine("  dotnet run --project .\\SqlTest\\SqlTest.csproj -- VH110,VZ475,VH111,VZ421");

        Console.WriteLine("  dotnet run --project .\\SqlTest\\SqlTest.csproj -- VH110 VZ475 VH111 VZ421");

        Console.WriteLine();

        Console.WriteLine("Notes:");

        Console.WriteLine("  - This tool reads base SQL from Copy_SQL_query.txt (ends with ... IN)");

        Console.WriteLine("  - It builds the Excel-style B2 list from your flat list values");

        Console.WriteLine("  - It uses the connectionString from config.json (same as the app)");

    }

}
 
         //  "Provider=SQLOLEDB;" +
        //   "Data Source=pbebdsmessier.nos.boeing.com;" +
          // "Initial Catalog=Newton_Release;" +
         //  "User ID=SvcIedwhExcel;" +
         //  "Password=0x9F4DE51F3477553AE46A752DD6B66FCE;";
     
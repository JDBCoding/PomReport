using System;

using System.Collections.Generic;

using System.Linq;

namespace PomReport.Data.Sql

{

    public static class SqlQueries

    {

        // IMPORTANT:

        // Replace dbo.YourTable and column names with your real schema.

        // This template is syntactically valid and supports {IN_CLAUSE}.

        public const string MainQueryTemplate = @"

WITH JobList AS

(

    SELECT *

    FROM dbo.YourTable

)

SELECT *

FROM JobList jl

WHERE 1 = 1

  AND jl.Program = '842'

  AND jl.SourceSystem = 'MES'

  AND jl.MbuName = 'EDC'

  AND jl.Status = 'Open'

  AND jl.LineNumber IN {IN_CLAUSE};

";

        public static string BuildLineNumberInClause(IEnumerable<string> lineNumbers)

        {

            if (lineNumbers == null)

                return "('NONE')";

            var cleaned = lineNumbers

                .Select(x => (x ?? string.Empty).Trim())

                .Where(x => !string.IsNullOrWhiteSpace(x))

                .Distinct(StringComparer.OrdinalIgnoreCase)

                .ToList();

            if (cleaned.Count == 0)

                return "('NONE')";

            // escape single quotes for SQL string literals

            var quoted = cleaned.Select(x => $"'{x.Replace("'", "''")}'");

            return "(" + string.Join(", ", quoted) + ")";

        }

        public static string BuildMainQuery(string lineNumberInClause)

        {

            if (string.IsNullOrWhiteSpace(lineNumberInClause))

                lineNumberInClause = "('NONE')";

            return MainQueryTemplate.Replace("{IN_CLAUSE}", lineNumberInClause);

        }

    }

}
 
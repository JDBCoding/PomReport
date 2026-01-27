namespace PomReport.Data.Sql
{
   public static class SqlQueries
   {
       // Your main query template. Keep {LINE_NUMBER_PARAMS} exactly like this.
       public const string MainQuery = @"
-- (your SQL here)
-- Make sure the WHERE clause includes something like:
-- AND jl.LineNumber IN ({LINE_NUMBER_PARAMS})
SELECT
   jl.LineNumber,
   jl.JobNumber,
   jl.Operation,
   jl.PartNumber,
   jl.Description
FROM dbo.JobLog jl
WHERE 1 = 1
 AND jl.LineNumber IN ({LINE_NUMBER_PARAMS})
;";
       /// <summary>
       /// Back-compat for older Form1.cs that expects SqlQueries.Current
       /// </summary>
       public static string Current => MainQuery;
       /// <summary>
       /// Back-compat for older Form1.cs that expects SqlQueries.BuildJobListQuery(...)
       /// If you pass "{LINE_NUMBER_PARAMS}", you get the raw template back.
       /// Otherwise, it substitutes the placeholder with your formatted list like:  'VH110','VZ475',...
       /// </summary>
       public static string BuildJobListQuery(string lineNumberParams)
       {
           if (string.IsNullOrWhiteSpace(lineNumberParams) ||
               lineNumberParams.Trim() == "{LINE_NUMBER_PARAMS}")
           {
               return MainQuery;
           }
           return MainQuery.Replace("{LINE_NUMBER_PARAMS}", lineNumberParams);
       }
   }
}
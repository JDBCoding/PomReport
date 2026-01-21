namespace PomReport.Data.Sql
{
   public sealed class SqlConnectionTestResult
   {
       public bool Success { get; set; }
       public string Server { get; set; } = "";
       public string Database { get; set; } = "";
       public string SystemUser { get; set; } = "";
       public string LoginName { get; set; } = "";
       public string ErrorMessage { get; set; } = "";
   }
}
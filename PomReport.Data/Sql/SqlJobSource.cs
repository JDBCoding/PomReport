using System;
using Microsoft.Data.SqlClient;
namespace PomReport.Data.Sql
{
   public static class SqlJobSource
   {
       // Keep this if you want a default/fallback connection
       public static string GetConn()
       {
           return
               "Server=pbebdsmessier.nos.boeing.com;" +
               "Database=Newton_Release;" +
               "User ID=SvcIedwhExcel;" +
               "Password=0x9F4DE51F3477553AE46A752DD6B66FCEE;" +
               "Encrypt=True;" +
               "TrustServerCertificate=True;";
       }
       // ✅ Single-argument test method (matches what we want everywhere)
       public static SqlConnectionTestResult TestConnection(string connString)
       {
           var result = new SqlConnectionTestResult();
           try
           {
               using var conn = new SqlConnection(connString);
               conn.Open();
               using var cmd = conn.CreateCommand();
               cmd.CommandText =
                   "SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DbName, SYSTEM_USER AS SystemUser, SUSER_SNAME() AS LoginName";
               using var reader = cmd.ExecuteReader();
               if (!reader.Read())
                   throw new Exception("Connection succeeded but the test query returned no rows.");
               result.Success = true;
               result.Server = reader["ServerName"]?.ToString() ?? "";
               result.Database = reader["DbName"]?.ToString() ?? "";
               result.SystemUser = reader["SystemUser"]?.ToString() ?? "";
               result.LoginName = reader["LoginName"]?.ToString() ?? "";
           }
           catch (Exception ex)
           {
               result.Success = false;
               result.ErrorMessage = ex.Message;
           }
           return result;
       }
   }
}
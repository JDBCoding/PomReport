using System;
using Microsoft.Data.SqlClient;
class Program
{
   static void Main()
   {
       var cs = Environment.GetEnvironmentVariable("POM_CS");
       if (string.IsNullOrWhiteSpace(cs))
       {
           Console.WriteLine("POM_CS env var not set.");
           return;
       }
       var b = new SqlConnectionStringBuilder(cs);
       b.Password = "*****";
       Console.WriteLine("Connecting with: " + b.ConnectionString);
       using var conn = new SqlConnection(cs);
       conn.Open();
       using var cmd = new SqlCommand("SELECT @@VERSION;", conn);
       var v = cmd.ExecuteScalar();
       Console.WriteLine(v);
   }
}
using System;
using System.Data.OleDb;
class Program
{
   static void Main()
   {
       var cs =
           "Provider=SQLOLEDB;" +
           "Data Source=pbebdsmessier.nos.boeing.com;" +
           "Initial Catalog=Newton_Release;" +
           "User ID=SvcIedwhExcel;" +
           "Password=0x9F4DE51F3477553AE46A752DD6B66FCE;";
       Console.WriteLine("Attempting OLE DB connection (Excel-style)...");
       try
       {
           using var conn = new OleDbConnection(cs);
           conn.Open();
           using var cmd = new OleDbCommand("SELECT @@VERSION", conn);
           var v = cmd.ExecuteScalar();
           Console.WriteLine("SUCCESS");
           Console.WriteLine(v);
       }
       catch (Exception ex)
       {
           Console.WriteLine("FAILED");
           Console.WriteLine(ex.GetType().FullName);
           Console.WriteLine(ex.Message);
       }
   }
}
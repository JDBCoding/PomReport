using System;

using System.Data.OleDb;

namespace PomReport.App.Diagnostics

{

    internal static class DbSmokeTest

    {

        public static void Run()

        {

            var cs =

                "Provider=SQLOLEDB;" +

                "Data Source=YOURSERVER;" +

                "Initial Catalog=YOURDB;" +

                "User ID=YOURUSER;" +

                "Password=YOURPASS;";

            Console.WriteLine("Attempting OLE DB connection...");

            using var conn = new OleDbConnection(cs);

            conn.Open();

            using var cmd = new OleDbCommand("SELECT @@VERSION", conn);

            var v = cmd.ExecuteScalar();

            Console.WriteLine("SUCCESS");

            Console.WriteLine(v);

        }

    }

}
 
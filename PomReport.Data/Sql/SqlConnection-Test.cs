using System;

using Microsoft.Data.SqlClient;

namespace PomReport.Data.Sql

{

    public static class SqlConnectionTest

    {

        public static void TestOpen(string connString)

        {

            using var conn = new SqlConnection(connString);

            conn.Open(); // If this succeeds, networking + creds + TLS are all good.

        }

        public static string WhoAmI(string connString)

        {

            using var conn = new SqlConnection(connString);

            conn.Open();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"

SELECT

    @@SERVERNAME AS ServerName,

    DB_NAME() AS CurrentDb,

    SYSTEM_USER AS SystemUser,

    SUSER_SNAME() AS LoginName;

";

            using var r = cmd.ExecuteReader();

            if (!r.Read())

                return "Connected, but identity query returned no rows.";

            var server = r["ServerName"]?.ToString() ?? "";

            var db = r["CurrentDb"]?.ToString() ?? "";

            var sys = r["SystemUser"]?.ToString() ?? "";

            var login = r["LoginName"]?.ToString() ?? "";

            return $"CONNECTED ✓  Server={server}  Database={db}  SYSTEM_USER={sys}  SUSER_SNAME()={login}";

        }

    }

}
 
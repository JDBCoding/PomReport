using System;

using System.Windows.Forms;

// IMPORTANT: make sure this matches where SqlJobSource lives.

// If SqlJobSource is in PomReport.Data.Sql namespace, keep this using.

// If it’s in a different namespace, adjust accordingly.

using PomReport.Data.Sql;

namespace PomReport.App

{

    public partial class Form1 : Form

    {

        public Form1()

        {

            InitializeComponent();

        }

        // This method MUST exist because Form1.Designer.cs is wired to it.

        private void btnPull_Click(object sender, EventArgs e)

        {

            try

            {

                // 1) Get the connection string (from your local file / whatever your SqlJobSource does)

                string connString = SqlJobSource.GetConn();

                // 2) Test the connection + identity

                var result = SqlJobSource.TestConnection(connString);

                // 3) Show what we connected as

                MessageBox.Show(

                    $"CONNECTED ✅\n\n" +

                    $"Server: {result.Server}\n" +

                    $"Database: {result.Database}\n" +

                    $"SYSTEM_USER: {result.SystemUser}\n" +

                    $"SUSER_SNAME(): {result.LoginName}",

                    "SQL Test",

                    MessageBoxButtons.OK,

                    MessageBoxIcon.Information

                );

                // TODO: after this passes, we can run your actual pull/report logic here.

            }

            catch (Exception ex)

            {

                MessageBox.Show(

                    $"FAILED ❌\n\n{ex.Message}",

                    "SQL Test",

                    MessageBoxButtons.OK,

                    MessageBoxIcon.Error

                );

            }

        }

    }

}
 
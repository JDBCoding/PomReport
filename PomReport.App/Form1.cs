using System;

using System.Linq;

using System.Windows.Forms;

using PomReport.Config;

namespace PomReport.App

{

    public partial class Form1 : Form

    {

        public Form1()

        {

            InitializeComponent();

        }

        // This method MUST exist because Form1.Designer.cs is wired to it.

        // Right now it will:

        // 1) Ensure config exists (launch SetupForm if not)

        // 2) Load config and write what we have into your log textbox

        private void btnPull_Click(object sender, EventArgs e)

        {

            try

            {

                // Step 1: Ensure config exists (first run -> setup)

                if (!ConfigStore.Exists())

                {

                    using var setup = new SetupForm();

                    if (setup.ShowDialog(this) != DialogResult.OK)

                        return; // user cancelled

                }

                // Step 2: Load config

                var cfg = ConfigStore.Load();

                // Step 3: Display something useful in your existing UI

                // Your designer shows a control named "log"

                // and a preview textbox named "textQueryPreview" (optional)

                log.Clear();

                log.AppendText($"Shop: {cfg.ShopName}{Environment.NewLine}");

                log.AppendText($"Config: {ConfigStore.ConfigPath}{Environment.NewLine}{Environment.NewLine}");

                log.AppendText("VH/VZ pairs:" + Environment.NewLine);

                foreach (var a in cfg.Airplanes)

                {

                    var loc = string.IsNullOrWhiteSpace(a.Location) ? "" : $" | {a.Location}";

                    log.AppendText($"  {a.Vh} = {a.Vz}{loc}{Environment.NewLine}");

                }

                log.AppendText(Environment.NewLine + "Job Categories:" + Environment.NewLine);

                foreach (var j in cfg.JobCategories)

                {

                    log.AppendText($"  {j.Ip} = {j.Category}{Environment.NewLine}");

                }

                // Optional: show a quick “next step” query preview placeholder

                // (only if your form has textQueryPreview)

                if (textQueryPreview != null)

                {

                    var vhList = cfg.Airplanes.Select(x => x.Vh).ToList();

                    var vzList = cfg.Airplanes.Select(x => x.Vz).ToList();

                    textQueryPreview.Text =

                        "-- NEXT: We'll parameterize this properly. For now, proof of inputs.\r\n" +

                        $"-- VH count: {vhList.Count}, VZ count: {vzList.Count}\r\n" +

                        $"-- Example VH: {string.Join(", ", vhList.Take(5))}\r\n" +

                        $"-- Example VZ: {string.Join(", ", vzList.Take(5))}\r\n";

                }

            }

            catch (Exception ex)

            {

                MessageBox.Show(ex.Message, "PomReport", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

    }

}
 
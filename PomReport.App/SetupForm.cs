using System;

using System.Linq;

using System.Windows.Forms;

using PomReport.Config;

using PomReport.Config.Models;

namespace PomReport.App

{

    public partial class SetupForm : Form

    {

        public SetupForm()

        {

            InitializeComponent();

        }

        private TextBox txtShop = null!;

        private TextBox txtAirplanes = null!;

        private TextBox txtCategories = null!;

        private Button btnSave = null!;

        private Button btnCancel = null!;

        private void InitializeComponent()

        {

            this.Text = "PomReport - First Run Setup";

            this.Width = 900;

            this.Height = 700;

            this.StartPosition = FormStartPosition.CenterScreen;

            var lblShop = new Label { Left = 15, Top = 15, Width = 250, Text = "Shop Name" };

            txtShop = new TextBox { Left = 15, Top = 40, Width = 400 };

            var lblAir = new Label

            {

                Left = 15,

                Top = 80,

                Width = 800,

                Text = "VH/VZ Pairs (one per line). Format: VH123=VZ901|Optional Location"

            };

            txtAirplanes = new TextBox

            {

                Left = 15,

                Top = 105,

                Width = 840,

                Height = 220,

                Multiline = true,

                ScrollBars = ScrollBars.Vertical

            };

            var lblCat = new Label

            {

                Left = 15,

                Top = 340,

                Width = 800,

                Text = "Job Category Mapping (one per line). Format: IP123=CategoryName"

            };

            txtCategories = new TextBox

            {

                Left = 15,

                Top = 365,

                Width = 840,

                Height = 220,

                Multiline = true,

                ScrollBars = ScrollBars.Vertical

            };

            btnSave = new Button { Left = 15, Top = 600, Width = 160, Text = "Save Setup" };

            btnCancel = new Button { Left = 190, Top = 600, Width = 160, Text = "Cancel" };

            btnSave.Click += BtnSave_Click;

            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lblShop);

            this.Controls.Add(txtShop);

            this.Controls.Add(lblAir);

            this.Controls.Add(txtAirplanes);

            this.Controls.Add(lblCat);

            this.Controls.Add(txtCategories);

            this.Controls.Add(btnSave);

            this.Controls.Add(btnCancel);

            // helpful defaults

            txtAirplanes.Text = "VH123=VZ901|Position 1\r\nVH124=VZ910|Position 2";

            txtCategories.Text = "IP100=Hydraulics\r\nIP200=Electrical";

        }

        private void BtnSave_Click(object? sender, EventArgs e)

        {

            try

            {

                var cfg = new ShopConfig

                {

                    ShopName = (txtShop.Text ?? "").Trim()

                };

                cfg.Airplanes = ParseAirplanes(txtAirplanes.Text);

                cfg.JobCategories = ParseCategories(txtCategories.Text);

                if (cfg.Airplanes.Count == 0)

                    throw new Exception("No VH/VZ pairs entered.");

                ConfigStore.Save(cfg);

                MessageBox.Show(

                    $"Saved config to:\n\n{ConfigStore.ConfigPath}",

                    "PomReport",

                    MessageBoxButtons.OK,

                    MessageBoxIcon.Information

                );

                this.DialogResult = DialogResult.OK;

                this.Close();

            }

            catch (Exception ex)

            {

                MessageBox.Show(ex.Message, "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        private static System.Collections.Generic.List<AirplanePair> ParseAirplanes(string text)

        {

            var list = new System.Collections.Generic.List<AirplanePair>();

            var lines = (text ?? "")

                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)

                .Select(l => l.Trim())

                .Where(l => !string.IsNullOrWhiteSpace(l))

                .ToList();

            foreach (var line in lines)

            {

                // VH123=VZ901|Location optional

                var parts = line.Split('|');

                var pairPart = parts[0].Trim();

                var location = parts.Length > 1 ? parts[1].Trim() : "";

                var kv = pairPart.Split('=');

                if (kv.Length != 2)

                    throw new Exception($"Invalid airplane line: '{line}'. Use VH###=VZ###|Optional Location");

                var vh = kv[0].Trim();

                var vz = kv[1].Trim();

                if (string.IsNullOrWhiteSpace(vh) || string.IsNullOrWhiteSpace(vz))

                    throw new Exception($"Invalid VH/VZ in line: '{line}'.");

                list.Add(new AirplanePair { Vh = vh, Vz = vz, Location = location });

            }

            // 1:1 enforcement

            if (list.GroupBy(x => x.Vh, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))

                throw new Exception("Duplicate VH found. Each VH must appear only once.");

            if (list.GroupBy(x => x.Vz, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))

                throw new Exception("Duplicate VZ found. Each VZ must appear only once.");

            return list;

        }

        private static System.Collections.Generic.List<JobCategoryMap> ParseCategories(string text)

        {

            var list = new System.Collections.Generic.List<JobCategoryMap>();

            var lines = (text ?? "")

                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)

                .Select(l => l.Trim())

                .Where(l => !string.IsNullOrWhiteSpace(l))

                .ToList();

            foreach (var line in lines)

            {

                var kv = line.Split('=');

                if (kv.Length != 2)

                    throw new Exception($"Invalid category line: '{line}'. Use IP###=CategoryName");

                var ip = kv[0].Trim();

                var cat = kv[1].Trim();

                if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(cat))

                    throw new Exception($"Invalid IP/Category in line: '{line}'.");

                list.Add(new JobCategoryMap { Ip = ip, Category = cat });

            }

            return list;

        }

    }

}
 
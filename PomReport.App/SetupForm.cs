using System;

using System.IO;

using System.Linq;

using System.Collections.Generic;

using System.Text.Json;

using System.Windows.Forms;

using PomReport.Config;

using PomReport.Config.Models;

using PomReport.Core.Models;

using PomReport.Core.Services;

namespace PomReport.App

{

    public partial class SetupForm : Form

    {

        private TextBox txtShop = null!;

        private TextBox txtAirplanes = null!;

        private TextBox txtCategories = null!;

        private Button btnSave = null!;

        private Button btnCancel = null!;

        private Button btnPull = null!;

        private Button btnTestPipeline = null!;

        public SetupForm()

        {

            InitializeComponent();

        }

        private void InitializeComponent()

        {

            Text = "setup form testing";
            
            Height = 760;

            StartPosition = FormStartPosition.CenterScreen;

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

            btnSave = new Button { Left = 15, Top = 610, Width = 160, Text = "Save Setup" };

            btnCancel = new Button { Left = 190, Top = 610, Width = 160, Text = "Cancel" };

            btnPull = new Button { Left = 370, Top = 610, Width = 160, Text = "Pull Data" };

            btnTestPipeline = new Button { Left = 545, Top = 610, Width = 200, Text = "Test Pipeline" };

            btnSave.Click += BtnSave_Click;

            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            btnPull.Click += Btnpull_click;

            btnTestPipeline.Click += btnTestPipeline_Click;

            Controls.Add(lblShop);

            Controls.Add(txtShop);

            Controls.Add(lblAir);

            Controls.Add(txtAirplanes);

            Controls.Add(lblCat);

            Controls.Add(txtCategories);

            Controls.Add(btnSave);

            Controls.Add(btnCancel);

            Controls.Add(btnPull);

            Controls.Add(btnTestPipeline);

            // Helpful defaults

            txtAirplanes.Text = "VH110=VZ475|STALL 212\r\nVH111=VZ421|STALL 213";

            txtCategories.Text = "IP100=Hydraulics\r\nIP200=Electrical";

        }

        private async void Btnpull_click(object? sender, EventArgs e)

        {

            MessageBox.Show(

                "SQL pull wiring comes next.\r\nThis button is intentionally live but not implemented yet.",

                "PomReport",

                MessageBoxButtons.OK,

                MessageBoxIcon.Information

            );

            await Task.CompletedTask;

        }

        private async void btnTestPipeline_Click(object? sender, EventArgs e)

        {

            try

            {

                var baseDir = Path.Combine(AppContext.BaseDirectory, "data");

                var snapDir = Path.Combine(baseDir, "snapshots");

                var cfgDir = Path.Combine(baseDir, "config");

                Directory.CreateDirectory(snapDir);

                Directory.CreateDirectory(cfgDir);

                var store = new SnapshotStore(snapDir);

                var baseline = FakeDataFactory.BaselineSnapshot();

                var current = FakeDataFactory.CurrentSnapshot();

                await store.SaveSnapshotAsync(baseline);

                await store.SaveSnapshotAsync(current);

                var chosenBaseline = await store.GetBaselineSnapshotAsync(

                    TimeSpan.FromHours(4),

                    DateTimeOffset.UtcNow

                );

                var jobSortMap = FakeDataFactory.JobSortRules()

                    .ToDictionary(x => x.JobNumber, x => x);

                var compare = new CompareEngine()

                    .Compare(current, chosenBaseline, jobSortMap);

                var clean = new CleanDataBuilder()

                    .Build(current, compare, jobSortMap, new Dictionary<string, LineStatusEntry>());

                var outPath = Path.Combine(baseDir, "clean_preview.json");

                await File.WriteAllTextAsync(

                    outPath,

                    JsonSerializer.Serialize(clean, new JsonSerializerOptions { WriteIndented = true })

                );

                MessageBox.Show(

                    $"Pipeline OK\r\n\r\n" +

                    $"New: {compare.NewJobs.Count}\r\n" +

                    $"Completed: {compare.CompletedJobs.Count}\r\n" +

                    $"Sold: {compare.SoldJobs.Count}\r\n\r\n" +

                    $"Output:\r\n{outPath}",

                    "Pipeline Test",

                    MessageBoxButtons.OK,

                    MessageBoxIcon.Information

                );

            }

            catch (Exception ex)

            {

                MessageBox.Show(ex.ToString(), "Pipeline Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        private void BtnSave_Click(object? sender, EventArgs e)

        {

            try

            {

                var cfg = new ShopConfig

                {

                    ShopName = (txtShop.Text ?? "").Trim(),

                    Airplanes = ParseAirplanes(txtAirplanes.Text),

                    JobCategories = ParseCategories(txtCategories.Text)

                };

                if (cfg.Airplanes.Count == 0)

                    throw new Exception("No VH/VZ pairs entered.");

                ConfigStore.Save(cfg);

                MessageBox.Show(

                    $"Saved config to:\r\n\r\n{ConfigStore.ConfigPath}",

                    "PomReport",

                    MessageBoxButtons.OK,

                    MessageBoxIcon.Information

                );

            }

            catch (Exception ex)

            {

                MessageBox.Show(ex.Message, "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        private static List<AirplanePair> ParseAirplanes(string text)

        {

            var list = new List<AirplanePair>();

            var lines = (text ?? "")

                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)

                .Select(l => l.Trim())

                .Where(l => !string.IsNullOrWhiteSpace(l));

            foreach (var line in lines)

            {

                var parts = line.Split('|');

                var kv = parts[0].Split('=');

                if (kv.Length != 2)

                    throw new Exception($"Invalid airplane line: {line}");

                list.Add(new AirplanePair

                {

                    Vh = kv[0].Trim(),

                    Vz = kv[1].Trim(),

                    Location = parts.Length > 1 ? parts[1].Trim() : ""

                });

            }

            return list;

        }

        private static List<JobCategoryMap> ParseCategories(string text)

        {

            var list = new List<JobCategoryMap>();

            var lines = (text ?? "")

                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)

                .Select(l => l.Trim())

                .Where(l => !string.IsNullOrWhiteSpace(l));

            foreach (var line in lines)

            {

                var kv = line.Split('=');

                if (kv.Length != 2)

                    throw new Exception($"Invalid category line: {line}");

                list.Add(new JobCategoryMap

                {

                    Ip = kv[0].Trim(),

                    Category = kv[1].Trim()

                });

            }

            return list;

        }

    }

}
 
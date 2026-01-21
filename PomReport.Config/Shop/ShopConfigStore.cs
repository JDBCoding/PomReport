using System;

using System.IO;

using System.Text.Json;

namespace PomReport.Config.Shop

{

    public static class ShopConfigStore

    {

        public static string BaseFolder =>

            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PomReport", "shops");

        public static string GetShopFolder(string shopName)

        {

            var safe = MakeSafeFolderName(shopName);

            return Path.Combine(BaseFolder, safe);

        }

        public static string GetConfigPath(string shopName)

        {

            return Path.Combine(GetShopFolder(shopName), "config.json");

        }

        public static ShopConfig LoadOrCreate(string shopName)

        {

            var path = GetConfigPath(shopName);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            if (!File.Exists(path))

            {

                var cfg = CreateDefault(shopName);

                Save(cfg);

                return cfg;

            }

            var json = File.ReadAllText(path);

            var cfgLoaded = JsonSerializer.Deserialize<ShopConfig>(json, JsonOptions())

                            ?? CreateDefault(shopName);

            // Ensure shop name stays consistent

            cfgLoaded.ShopName = shopName;

            return cfgLoaded;

        }

        public static void Save(ShopConfig cfg)

        {

            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            var path = GetConfigPath(cfg.ShopName);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var json = JsonSerializer.Serialize(cfg, JsonOptions());

            File.WriteAllText(path, json);

        }

        private static ShopConfig CreateDefault(string shopName)

        {

            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            return new ShopConfig

            {

                ShopName = shopName,

                Paths =

                {

                    SnapshotFolder = Path.Combine(docs, "PomReport", "Snapshots"),

                    ReportFolder = Path.Combine(docs, "PomReport", "Reports"),

                    RetentionDays = 30

                }

            };

        }

        private static JsonSerializerOptions JsonOptions() => new()

        {

            WriteIndented = true,

            PropertyNameCaseInsensitive = true

        };

        private static string MakeSafeFolderName(string name)

        {

            foreach (var c in Path.GetInvalidFileNameChars())

                name = name.Replace(c, '_');

            return string.IsNullOrWhiteSpace(name) ? "Shop" : name.Trim();

        }

    }

}
 
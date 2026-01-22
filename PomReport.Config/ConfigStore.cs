using System;
using System.IO;
using System.Text.Json;
using PomReport.Config.Models;
namespace PomReport.Config
{
   public static class ConfigStore
   {
       private static readonly JsonSerializerOptions JsonOptions = new()
       {
           WriteIndented = true,
           PropertyNameCaseInsensitive = true
       };
       // EXE folder (UNC share folder)
       public static string AppFolder => AppContext.BaseDirectory;
       public static string ConfigPath => Path.Combine(AppFolder, "config.json");
       public static bool Exists() => File.Exists(ConfigPath);
       public static ShopConfig Load()
       {
           if (!File.Exists(ConfigPath))
               throw new FileNotFoundException("config.json not found", ConfigPath);
           var json = File.ReadAllText(ConfigPath);
           var cfg = JsonSerializer.Deserialize<ShopConfig>(json, JsonOptions) ?? new ShopConfig();
           cfg.Airplanes ??= new();
           cfg.JobCategories ??= new();
           return cfg;
       }
       public static void Save(ShopConfig cfg)
       {
           cfg.LastUpdatedUtc = DateTime.UtcNow;
           var json = JsonSerializer.Serialize(cfg, JsonOptions);
           File.WriteAllText(ConfigPath, json);
           // Ensure standard folders exist
           Directory.CreateDirectory(Path.Combine(AppFolder, cfg.ExportFolderName));
           Directory.CreateDirectory(Path.Combine(AppFolder, cfg.SnapshotFolderName));
       }
       public static string GetExportFolder(ShopConfig cfg)
           => Path.Combine(AppFolder, cfg.ExportFolderName);
       public static string GetSnapshotFolder(ShopConfig cfg)
           => Path.Combine(AppFolder, cfg.SnapshotFolderName);
   }
}
using System;
using System.IO;
using System.Text.Json;
using PomReport.Config.Models;
namespace PomReport.Config
{
   public static class ConfigStore
   {
       // Next to the running EXE (portable)
       public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "config.json");
       public static bool Exists() => File.Exists(ConfigPath);
       public static ShopConfig Load()
       {
           if (!Exists())
               throw new FileNotFoundException("Config not found.", ConfigPath);
           var json = File.ReadAllText(ConfigPath);
           var cfg = JsonSerializer.Deserialize<ShopConfig>(json, JsonOptions());
           if (cfg == null)
               throw new InvalidOperationException("Config file is empty or invalid JSON.");
           return cfg;
       }
       public static void Save(ShopConfig cfg)
       {
           var json = JsonSerializer.Serialize(cfg, JsonOptions());
           File.WriteAllText(ConfigPath, json);
       }
       public static void CreateDefaultIfMissing()
       {
           if (Exists()) return;
           var cfg = new ShopConfig
           {
               ShopName = "New Shop"
           };
           Save(cfg);
       }
       private static JsonSerializerOptions JsonOptions() => new JsonSerializerOptions
       {
           PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
           WriteIndented = true
       };
   }
}
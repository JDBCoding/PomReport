using System;
using System.Collections.Generic;
namespace PomReport.Config.Models
{
   public sealed class ShopConfig
   {
       public string ShopName { get; set; } = "";
       // Since we run from the UNC folder, everything is relative to EXE folder by default.
       public string ExportFolderName { get; set; } = "exports";
       public string SnapshotFolderName { get; set; } = "snapshots";
       // ✅ Add this so config.json can hold the SQL connection string
       public string ConnectionString { get; set; } = "";
       public List<AirplanePair> Airplanes { get; set; } = new();
       public List<JobCategoryMap> JobCategories { get; set; } = new();
       public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
   }
}
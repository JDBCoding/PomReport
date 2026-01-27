using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PomReport.Config.Models;

namespace PomReport.Config.Models
{
   public class ShopConfig
   {
       [JsonPropertyName("shopName")]
       public string? ShopName { get; set; }
       [JsonPropertyName("exportFolderName")]
       public string? ExportFolderName { get; set; }
       [JsonPropertyName("snapshotFolderName")]
       public string? SnapshotFolderName { get; set; }
       // IMPORTANT: matches "connectionString" in config.json
       [JsonPropertyName("connectionString")]
       public string? ConnectionString { get; set; }
       [JsonPropertyName("airplanes")]
       public List<PomReport.Config.Models.AirplanePair> Airplanes { get; set; } = new();
       [JsonPropertyName("jobCategories")]
       public List<JobCategoryMap> JobCategories { get; set; } = new();
       [JsonPropertyName("lastUpdatedUtc")]
       public DateTime? LastUpdatedUtc { get; set; }
   }
}
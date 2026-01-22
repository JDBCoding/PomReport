using System;
using System.Collections.Generic;
namespace PomReport.Config.Shop
{
   public sealed class ShopConfig
   {
       public string ShopName { get; set; } = "My Shop";
       public List<AirplaneConfig> Airplanes { get; set; } = new();
       public List<CategoryConfig> Categories { get; set; } = new();
       public PathConfig Paths { get; set; } = new();
       public EmailConfig Email { get; set; } = new();
       public DiffConfig Diff { get; set; } = new();
       public SqlConfig Sql { get; set; } = new();
   }
   public sealed class AirplaneConfig
   {
       public string LineNumber { get; set; } = "";   // VH###
       public string VzNumber { get; set; } = "";     // VZ### (optional)
       public string Location { get; set; } = "";
       public bool Enabled { get; set; } = true;
   }
   public sealed class CategoryConfig
   {
       public string Name { get; set; } = "";
       public List<CategoryRule> Rules { get; set; } = new();
   }
   public sealed class CategoryRule
   {
       // field examples: "JobKit", "JobKitDescription", "DailyPlan", "HeldFor"
       public string Field { get; set; } = "";
       // operator examples: "Equals", "Contains", "StartsWith"
       public string Operator { get; set; } = "Contains";
       public string Value { get; set; } = "";
   }
   public sealed class PathConfig
   {
       public string SnapshotFolder { get; set; } = "";
       public string ReportFolder { get; set; } = "";
       public int RetentionDays { get; set; } = 30;
   }
   public sealed class EmailConfig
   {
       public List<string> To { get; set; } = new();
       public List<string> Cc { get; set; } = new();
       public string SubjectTemplate { get; set; } = "{shopName} Shift {shift} POM Report - {date}";
       public string Mode { get; set; } = "Draft"; // Draft or Send (later)
   }
   public sealed class DiffConfig
   {
       public string StableKey { get; set; } = "JobId";
       public bool CompletedWhenMissing { get; set; } = true;
       public string CarryForward { get; set; } = "LatestCommentOnly";
       public List<string> UpdatedFields { get; set; } = new()
       {
           "JobComments","JobNotes","DailyPlan","HeldFor","Technicians"
       };
   }
   public sealed class SqlConfig
   {
       public string ConnectionString { get; set; } = "";
       public string Program { get; set; } = "842";
       public string SourceSystem { get; set; } = "MES";
       public string MbuName { get; set; } = "EDC";
       public string Status { get; set; } = "Open";
   }
}
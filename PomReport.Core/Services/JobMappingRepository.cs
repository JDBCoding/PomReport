using System;
using System.Collections.Generic;
using System.IO;
using PomReport.Core.Models;
namespace PomReport.Core.Services
{
   public class JobMappingRepository
   {
       public readonly string _filePath;
       public JobMappingRepository()
       {
           _filePath = Path.Combine(
               AppContext.BaseDirectory,
               "data",
               "config",
               "job_mapping.csv"
           );
       }
       public string FilePath => _filePath;
       public List<JobMappingRule> Load()
       {
           var results = new List<JobMappingRule>();
           if (!File.Exists(_filePath))
               return results;
           var lines = File.ReadAllLines(_filePath);
           if (lines.Length <= 1)
               return results;
           for (int i = 1; i < lines.Length; i++)
           {
               var parts = lines[i].Split(',');
               if (parts.Length < 3)
                   continue;
               results.Add(new JobMappingRule(
                    JobKit: parts[0].Trim(),
                    Category: parts[1].Trim(),
                    DisplayName: parts[2].Trim(),
                    SortOrder: parts.Length > 3 && int.TryParse(parts[3], out var s) ? s : 0,
                    Notes: parts.Length > 4 ? parts[4].Trim() : string.Empty
                ));
           }
           return results;
       }
       public void EnsureTemplateExists()
       {
           var dir = Path.GetDirectoryName(_filePath)!;
           Directory.CreateDirectory(dir);
           if (File.Exists(_filePath))
               return;
           File.WriteAllLines(_filePath, new[]
           {
               "JobKit,Category,DisplayName,SortOrder,Notes",
               "123-456,Hydraulics,Hydraulic Install,10,Example row",
               "ABC-999,Electrical,Wire Routing,20,Another example"
           });
       }
   }
}
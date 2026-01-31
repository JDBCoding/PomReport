using System.Collections.Generic;
using System.Linq;
using PomReport.Core.Core.Models;
namespace PomReport.Core.Diff
{
   public static class DiffEngine
   {
       public static DiffResult Diff(
           IReadOnlyList<JobRecord> previous,
           IReadOnlyList<JobRecord> current)
       {
           previous ??= new List<JobRecord>();
           current ??= new List<JobRecord>();
           var prevByKey = previous.ToDictionary(Key);
           var currByKey = current.ToDictionary(Key);
           var added = current
               .Where(j => !prevByKey.ContainsKey(Key(j)))
               .ToList();
           var completed = previous
               .Where(j => !currByKey.ContainsKey(Key(j)))
               .ToList();
           var updated = new List<(JobRecord OldJob, JobRecord NewJob)>();
           foreach (var kv in currByKey)
           {
               if (!prevByKey.TryGetValue(kv.Key, out var oldJob))
                   continue;
               var newJob = kv.Value;
               if (IsDifferent(oldJob, newJob))
               {
                   updated.Add((oldJob, newJob));
               }
           }
           var open = current
               .Where(j => !completed.Any(c => Key(c) == Key(j)))
               .ToList();
           return new DiffResult(
               added,
               completed,
               updated,
               open
           );
       }
       private static string Key(JobRecord j)
           => $"{j.LineNumber}|{j.WorkOrder}";
       private static bool IsDifferent(JobRecord a, JobRecord b)
       {
           return
               a.PlannedHours != b.PlannedHours ||
               a.ActualHours != b.ActualHours ||
               a.DailyPlan != b.DailyPlan ||
               a.HeldFor != b.HeldFor ||
               a.Technicians != b.Technicians ||
               a.JobNotes != b.JobNotes ||
               a.JobComments != b.JobComments ||
               a.Category != b.Category ||
               a.Location != b.Location;
       }
   }
}
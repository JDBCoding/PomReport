using System.Collections.Generic;
using PomReport.Core.Core.Models;
namespace PomReport.Data.Fake
{
   public static class FakeJobSource
   {
       public static List<JobRecord> GetSample()
       {
           return new List<JobRecord>
           {
               new JobRecord(
                   JobId: 1001,
                   LineNumber: "VH110",
                   WorkOrder: "WO-12345",
                   JobKitDescription: "Install bracket assembly",
                   JobNotes: "-",
                   PlannedHours: 12.5m,
                   JobComments: "Waiting on parts",
                   Technicians: "J. Doe, A. Smith",
                   DailyPlan: "WIP",
                   ActualHours: 3.0m,
                   JobKit: "KIT-001",
                   ParentWorkOrder: "-",
                   HeldFor: "Material",
                   Category: "IP-0001",
                   Location: "BoomShop"
               ),
               new JobRecord(
                   JobId: 1002,
                   LineNumber: "VZ475",
                   WorkOrder: "WO-54321",
                   JobKitDescription: "Torque check",
                   JobNotes: "Double-check torque spec",
                   PlannedHours: 4.0m,
                   JobComments: "-",
                   Technicians: "B. Johnson",
                   DailyPlan: "DCMT",
                   ActualHours: 4.0m,
                   JobKit: "KIT-002",
                   ParentWorkOrder: "WO-11111",
                   HeldFor: "-",
                   Category: "IP-0002",
                   Location: "Flightline"
               )
           };
       }
   }
}
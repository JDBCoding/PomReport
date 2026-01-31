using System;

namespace PomReport.Core.Core.Models;

public sealed record JobRecord(
   long JobId,
   string LineNumber,
   string WorkOrder,
   string JobKitDescription,
   string JobNotes,
   decimal PlannedHours,
   string JobComments,
   string Technicians,
   string DailyPlan,
   decimal ActualHours,
   string JobKit,
   string ParentWorkOrder,
   string HeldFor,
   string Category,
   string Location
);
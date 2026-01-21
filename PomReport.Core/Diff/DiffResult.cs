using System.Collections.Generic;
namespace PomReport.Core.Models;
public sealed record DiffResult(
   IReadOnlyList<JobRecord> Added,
   IReadOnlyList<JobRecord> Completed,
   IReadOnlyList<(JobRecord OldJob, JobRecord NewJob)> Updated,
   IReadOnlyList<JobRecord> Open
);
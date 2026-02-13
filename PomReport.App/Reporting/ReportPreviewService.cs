using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using PomReport.Config.Models;
using PomReport.Core.Core.Models;
using PomReport.Core.Models;
using PomReport.Core.Services;
using PomReport.Data.Csv;

namespace PomReport.App.Reporting;

/// <summary>
/// Generates an email-safe HTML preview report from exports/newdata.csv (and exports/olddata.csv if present).
/// Pulling data and generating the report are intentionally separate.
/// </summary>
public static class ReportPreviewService
{
    public static string GenerateHtmlPreview(
        string exportDir,
        string shopName,
        IReadOnlyList<AirplanePair> airplanePairs)
    {
        if (string.IsNullOrWhiteSpace(exportDir))
            throw new ArgumentException("exportDir is blank.", nameof(exportDir));

        Directory.CreateDirectory(exportDir);

        var newCsv = Path.Combine(exportDir, "newdata.csv");
        var oldCsv = Path.Combine(exportDir, "olddata.csv");
        var newJson = Path.Combine(exportDir, "newdata.airplanes.json");
        var oldJson = Path.Combine(exportDir, "olddata.airplanes.json");

        if (!File.Exists(newCsv))
            throw new FileNotFoundException("NEWDATA.CSV NOT FOUND. CLICK PULL DATA FIRST.", newCsv);

        var newJobsRaw = CsvJobSource.Load(newCsv);
        var oldJobsRaw = File.Exists(oldCsv) ? CsvJobSource.Load(oldCsv) : new List<JobRecord>();

        // Apply mapping rules for reporting (job_mapping.csv + NCR overrides) and normalize to uppercase.
        var mapper = new ReportJobMapper();
        var newJobs = newJobsRaw.Select(mapper.MapForReport).ToList();
        var oldJobs = oldJobsRaw.Select(mapper.MapForReport).ToList();

        // Diff rules for reporting:
        // - Key: LINENUMBER|WORKORDER
        // - Updated: JobComments changed ONLY
        // - JobNotes is display-only and ignored for diff
        var diff = ReportDiffEngine.Diff(oldJobs, newJobs);

        // Render HTML
        var html = HtmlReportRenderer.Render(
            shopName: shopName,
            exportDir: exportDir,
            diff: diff,
            airplanePairs: airplanePairs);

        var outPath = Path.Combine(exportDir, "pom_report_preview.html");
        File.WriteAllText(outPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        // First run: if baseline missing, seed olddata after generating the report.
        // Pull does NOT create olddata. Report generation does, so next run has meaningful deltas.
        if (!File.Exists(oldCsv))
        {
            File.Copy(newCsv, oldCsv, overwrite: true);
            if (File.Exists(newJson))
                File.Copy(newJson, oldJson, overwrite: true);

            File.SetLastWriteTimeUtc(oldCsv, DateTime.UtcNow);
            if (File.Exists(oldJson))
                File.SetLastWriteTimeUtc(oldJson, DateTime.UtcNow);
        }

        return outPath;
    }

    public static void OpenInDefaultBrowser(string htmlPath)
    {
        if (string.IsNullOrWhiteSpace(htmlPath))
            return;

        try
        {
            if (!File.Exists(htmlPath))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = htmlPath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Some locked-down environments block direct browser launch; fall back to Explorer.
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{htmlPath}\"",
                    UseShellExecute = true
                });
            }
            catch
            {
                // ignore
            }
        }
    }

    private sealed class ReportJobMapper
    {
        private readonly Dictionary<string, JobMappingRule> _jobKitMap;
        private readonly Dictionary<string, string> _ncrOverrides;

        public ReportJobMapper()
        {
            var jobRepo = new JobMappingRepository();
            jobRepo.EnsureTemplateExists();

            _jobKitMap = jobRepo
                .Load()
                .Where(r => !string.IsNullOrWhiteSpace(r.JobKit))
                .GroupBy(r => r.JobKit, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(r => r.JobKit.Trim().ToUpperInvariant(), r => r);

            _ncrOverrides = new NcrOverrideRepository(NcrOverrideRepository.DefaultPath()).Load();
        }

        public JobRecord MapForReport(JobRecord j)
        {
            var line = U(j.LineNumber);
            var wo = U(j.WorkOrder);
            var kit = U(j.JobKit);
            var desc = U(j.JobKitDescription);
            var notes = U(j.JobNotes);
            var comments = U(j.JobComments);
            var heldFor = U(j.HeldFor);
            var loc = U(j.Location);
            var category = U(j.Category);

            // Apply job_mapping.csv rule by JobKit.
            if (!string.IsNullOrWhiteSpace(kit) && _jobKitMap.TryGetValue(kit, out var rule))
            {
                if (!string.IsNullOrWhiteSpace(rule.Category))
                    category = U(rule.Category);

                // DisplayName is the friendly description, but JobNotes overrides everything.
                if (IsDashOrBlank(notes) && !string.IsNullOrWhiteSpace(rule.DisplayName))
                    desc = U(rule.DisplayName);
            }

            // JobNotes: display override only (NOT part of diff)
            var displayDesc = !IsDashOrBlank(notes) ? notes : desc;

            // NCR overrides: if the row has an NCR id and user categorized it, override category.
            var ncrId = NcrIdParser.TryExtract(displayDesc);
            if (!string.IsNullOrWhiteSpace(ncrId) && _ncrOverrides.TryGetValue(ncrId, out var overrideCat) && !string.IsNullOrWhiteSpace(overrideCat))
                category = U(overrideCat);

            return j with
            {
                LineNumber = line,
                WorkOrder = wo,
                JobKit = string.IsNullOrWhiteSpace(kit) ? "-" : kit,
                JobKitDescription = string.IsNullOrWhiteSpace(displayDesc) ? "-" : displayDesc,
                JobNotes = IsDashOrBlank(notes) ? "-" : notes,
                JobComments = IsDashOrBlank(comments) ? "-" : comments,
                HeldFor = IsDashOrBlank(heldFor) ? "-" : heldFor,
                Category = string.IsNullOrWhiteSpace(category) ? "-" : category,
                Location = IsDashOrBlank(loc) ? "-" : loc
            };
        }

        private static string U(string? s) => (s ?? string.Empty).Trim().ToUpperInvariant();
        private static bool IsDashOrBlank(string? s)
        {
            var t = (s ?? string.Empty).Trim();
            return t.Length == 0 || t == "-";
        }
    }
}

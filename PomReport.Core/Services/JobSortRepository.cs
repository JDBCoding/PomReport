using System.Text.Json;
using PomReportCore.Models;

namespace PomReportCore.Services;

public sealed class JobSortRepository
{
    private readonly string _path;

    public JobSortRepository(string path) => _path = path;

    public async Task<Dictionary<string, JobSortRule>> LoadMapAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path)) return new(StringComparer.OrdinalIgnoreCase);

        var json = await File.ReadAllTextAsync(_path, ct);
        var rules = JsonSerializer.Deserialize<List<JobSortRule>>(json, JsonOptions()) ?? new();

        return rules
            .Where(r => !string.IsNullOrWhiteSpace(r.JobNumber))
            .ToDictionary(r => r.JobNumber.Trim(), r => r, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SaveAsync(IEnumerable<JobSortRule> rules, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(rules, JsonOptions());
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await File.WriteAllTextAsync(_path, json, ct);
    }

    public static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

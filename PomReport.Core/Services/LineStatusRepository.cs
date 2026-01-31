using System.Text.Json;
using PomReportCore.Models;

namespace PomReportCore.Services;

public sealed class LineStatusRepository
{
    private readonly string _path;

    public LineStatusRepository(string path) => _path = path;

    public async Task<List<LineStatusEntry>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path)) return new();
        var json = await File.ReadAllTextAsync(_path, ct);
        return JsonSerializer.Deserialize<List<LineStatusEntry>>(json, JsonOptions()) ?? new();
    }

    public async Task SaveAsync(IEnumerable<LineStatusEntry> entries, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(entries, JsonOptions());
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await File.WriteAllTextAsync(_path, json, ct);
    }

    public static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

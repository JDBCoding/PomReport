using System.Text.Json;
using PomReportCore.Models;

namespace PomReportCore.Services;

public sealed class SnapshotStore
{
    private readonly string _dir;
    private readonly string _indexPath;

    public SnapshotStore(string directory)
    {
        _dir = directory;
        _indexPath = Path.Combine(_dir, "index.json");
        Directory.CreateDirectory(_dir);
    }

    public sealed record SnapshotIndexItem(string FileName, DateTimeOffset CreatedAtUtc);

    public async Task<JobSnapshot> SaveSnapshotAsync(JobSnapshot snapshot, CancellationToken ct = default)
    {
        snapshot.Normalized();
        snapshot.CreatedAtUtc = snapshot.CreatedAtUtc.ToUniversalTime();

        var fileName = $"snapshot_{snapshot.CreatedAtUtc:yyyy-MM-dd_HHmmss}.json";
        var fullPath = Path.Combine(_dir, fileName);

        var json = JsonSerializer.Serialize(snapshot, JsonOptions());
        await File.WriteAllTextAsync(fullPath, json, ct);

        var index = await LoadIndexAsync(ct);
        index.Add(new SnapshotIndexItem(fileName, snapshot.CreatedAtUtc));
        index = index.OrderBy(i => i.CreatedAtUtc).ToList();
        await SaveIndexAsync(index, ct);

        return snapshot;
    }

    public async Task<JobSnapshot?> LoadSnapshotAsync(string fileName, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_dir, fileName);
        if (!File.Exists(fullPath)) return null;

        var json = await File.ReadAllTextAsync(fullPath, ct);
        return JsonSerializer.Deserialize<JobSnapshot>(json, JsonOptions())?.Normalized();
    }

    public async Task<List<SnapshotIndexItem>> LoadIndexAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_indexPath)) return new();
        var json = await File.ReadAllTextAsync(_indexPath, ct);
        return JsonSerializer.Deserialize<List<SnapshotIndexItem>>(json, JsonOptions()) ?? new();
    }

    private async Task SaveIndexAsync(List<SnapshotIndexItem> items, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(items, JsonOptions());
        await File.WriteAllTextAsync(_indexPath, json, ct);
    }

    /// <summary>
    /// Option B: time-based baseline
    /// Baseline = newest snapshot with CreatedAtUtc <= nowUtc - window
    /// </summary>
    public async Task<JobSnapshot?> GetBaselineSnapshotAsync(TimeSpan window, DateTimeOffset nowUtc, CancellationToken ct = default)
    {
        var index = await LoadIndexAsync(ct);
        if (index.Count == 0) return null;

        var cutoff = nowUtc.ToUniversalTime().Subtract(window);

        var baselineItem = index
            .Where(i => i.CreatedAtUtc <= cutoff)
            .OrderByDescending(i => i.CreatedAtUtc)
            .FirstOrDefault();

        if (baselineItem is null) return null;
        return await LoadSnapshotAsync(baselineItem.FileName, ct);
    }

    public async Task PruneAsync(TimeSpan keepFor, DateTimeOffset nowUtc, CancellationToken ct = default)
    {
        var index = await LoadIndexAsync(ct);
        if (index.Count == 0) return;

        var cutoff = nowUtc.ToUniversalTime().Subtract(keepFor);
        var keep = index.Where(i => i.CreatedAtUtc >= cutoff).ToList();
        var remove = index.Except(keep).ToList();

        foreach (var item in remove)
        {
            var fullPath = Path.Combine(_dir, item.FileName);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        keep = keep.OrderBy(i => i.CreatedAtUtc).ToList();
        await SaveIndexAsync(keep, ct);
    }

    public static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

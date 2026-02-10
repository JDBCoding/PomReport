using System.Text.Json;

namespace PomReport.Core.Services;

/// <summary>
/// Persists user-chosen categories for NCR one-off jobs.
/// Stored as a simple JSON dictionary: { "NCR404352W": "CDS", ... }
/// </summary>
public sealed class NcrOverrideRepository
{
    private readonly string _path;

    public NcrOverrideRepository(string path) => _path = path;

    public static string DefaultPath() => Path.Combine(
        AppContext.BaseDirectory,
        "data",
        "config",
        "ncr_overrides.json");

    public Dictionary<string, string> Load()
    {
        if (!File.Exists(_path)) return new(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(_path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions())
                       ?? new Dictionary<string, string>();
            return new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    public void Save(Dictionary<string, string> map)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var json = JsonSerializer.Serialize(map, JsonOptions());
        File.WriteAllText(_path, json);
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };
}

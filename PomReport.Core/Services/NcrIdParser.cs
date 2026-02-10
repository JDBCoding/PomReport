using System.Text.RegularExpressions;

namespace PomReport.Core.Services;

/// <summary>
/// Extracts NCR identifiers from freeform text like:
/// "NCR: NCR404352W ..." or "NCR404352W".
/// </summary>
public static class NcrIdParser
{
    // NCR + 5-8 digits + optional trailing letter(s)
    private static readonly Regex Rx = new(@"\bNCR\s*:?\s*(NCR\d{5,8}[A-Z]{0,2})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RxBare = new(@"\b(NCR\d{5,8}[A-Z]{0,2})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? TryExtract(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var m = Rx.Match(text);
        if (m.Success) return m.Groups[1].Value.ToUpperInvariant();

        // Fallback if there isn't an "NCR:" prefix
        m = RxBare.Match(text);
        if (m.Success) return m.Groups[1].Value.ToUpperInvariant();

        return null;
    }
}

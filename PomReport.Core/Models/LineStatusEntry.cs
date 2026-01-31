namespace PomReport.Core.Models;

public sealed record LineStatusEntry(
    int Line,           // 1348
    string VH,          // VH110
    string VZ,          // VZ475
    string Stall,       // 212 or F1
    string Class);      // CLASS / UNCLASS

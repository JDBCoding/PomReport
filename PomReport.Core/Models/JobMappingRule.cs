using System;

namespace PomReport.Core.Models
{
    /// <summary>
    /// User-maintained mapping (sorting/categorization/rename) keyed by JobKit.
    /// JobKit comes from the SQL query as j.PartNumber AS JobKit.
    /// </summary>
    public sealed record JobMappingRule(
        string JobKit,
        string Category,
        string DisplayName,
        int SortOrder,
        string Notes)
    {
        public static JobMappingRule Empty(string jobKit) => new(
            JobKit: jobKit ?? string.Empty,
            Category: string.Empty,
            DisplayName: string.Empty,
            SortOrder: 0,
            Notes: string.Empty);
    }
}

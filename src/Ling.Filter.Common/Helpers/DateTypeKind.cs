namespace Ling.Filter.Helpers;

/// <summary>
/// Identifies supported BCL date/time types for precision-aware filtering.
/// </summary>
internal enum DateTypeKind
{
    None = 0,
    DateTime = 1,
    DateTimeOffset = 2,
    DateOnly = 3,
    TimeOnly = 4,
}

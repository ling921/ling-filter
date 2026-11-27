namespace Ling.Filter.Generators.Models;

/// <summary>
/// Value-equatable specification for a single default sort level (<c>OrderBy</c> / <c>ThenBy</c>).
/// </summary>
/// <param name="TargetPath">Resolved entity property path.</param>
/// <param name="Descending">Whether this sort level is descending.</param>
internal sealed record DefaultSortSpec(
    PropertyPathSpec TargetPath,
    bool Descending);

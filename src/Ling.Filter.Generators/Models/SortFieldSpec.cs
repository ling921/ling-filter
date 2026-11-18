namespace Ling.Filter.Generators.Models;

/// <summary>
/// Value-equatable specification for an allowed dynamic sort key and its resolved entity path.
/// </summary>
/// <param name="SortKeyLowerInvariant">Lowercase sort key matched against <c>SortBy</c>.</param>
/// <param name="TargetPath">Resolved entity property path.</param>
internal sealed record SortFieldSpec(
    string SortKeyLowerInvariant,
    PropertyPathSpec TargetPath);

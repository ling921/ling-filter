using System.Collections.Immutable;
using Ling.Filter.Extensions;
using Microsoft.CodeAnalysis;

namespace Ling.Filter.Helpers;

/// <summary>
/// Resolves dot-separated entity property paths and convention-based property mappings against an entity <see cref="ITypeSymbol"/>.
/// </summary>
internal static class PropertyPathResolver
{
    private static readonly (string Prefix, int OperatorValue)[] s_prefixConventions =
    [
        ("Min", 3),   // CompareOperator.GreaterThanOrEqual
        ("Start", 3), // CompareOperator.GreaterThanOrEqual
        ("Max", 5),   // CompareOperator.LessThanOrEqual
        ("End", 5),   // CompareOperator.LessThanOrEqual
    ];

    private static readonly (string Suffix, int OperatorValue)[] s_suffixConventions =
    [
        ("From", 3),  // CompareOperator.GreaterThanOrEqual
        ("To", 5),    // CompareOperator.LessThanOrEqual
    ];

    /// <summary>
    /// Attempts to resolve a dot-separated property path (such as <c>"Department.Manager.Name"</c>) starting from <paramref name="rootType"/>.
    /// </summary>
    public static bool TryResolvePath(
        Compilation compilation,
        ITypeSymbol rootType,
        string rawPath,
        out ImmutableArray<ResolvedPropertySegment> segments,
        out string? failureSegment)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            segments = [];
            failureSegment = rawPath;
            return false;
        }

        var parts = rawPath.Split('.');
        var builder = ImmutableArray.CreateBuilder<ResolvedPropertySegment>(parts.Length);
        var currentType = rootType;

        foreach (var rawPart in parts)
        {
            var part = rawPart.Trim();
            if (part.Length == 0)
            {
                segments = [];
                failureSegment = rawPath;
                return false;
            }

            var unwrappedCurrent = currentType.UnwrapNullableValueType(out _);
            var prop = FindAccessibleInstanceProperty(compilation, unwrappedCurrent, part);
            if (prop is null)
            {
                segments = [];
                failureSegment = part;
                return false;
            }

            builder.Add(new ResolvedPropertySegment(
                prop,
                prop.Name,
                prop.Type,
                prop.Type.CanBeNull()));

            currentType = prop.Type;
        }

        segments = builder.ToImmutable();
        failureSegment = null;
        return true;
    }

    /// <summary>
    /// Attempts to resolve an unannotated DTO property by exact name or optional naming conventions
    /// (<c>MinXxx</c>/<c>MaxXxx</c>/<c>StartXxx</c>/<c>EndXxx</c>/<c>XxxFrom</c>/<c>XxxTo</c>, <c>XxxRange</c>, and plural collection names).
    /// </summary>
    public static bool TryResolveConventionTarget(
        Compilation compilation,
        ITypeSymbol entityType,
        IPropertySymbol dtoProperty,
        bool autoMapSameName,
        bool enableNamingConventions,
        out ImmutableArray<ResolvedPropertySegment> segments,
        out int? inferredCompareOperator)
    {
        inferredCompareOperator = null;
        var propName = dtoProperty.Name;

        // 1. Exact same-name property on entity (case-insensitive fallback if exact case doesn't match)
        if (autoMapSameName)
        {
            if (TryResolvePath(compilation, entityType, propName, out segments, out _))
            {
                return true;
            }

            var caseInsensitive = FindAccessibleInstanceProperty(compilation, entityType, propName, ignoreCase: true);
            if (caseInsensitive is not null)
            {
                segments =
                [
                    new ResolvedPropertySegment(
                        caseInsensitive,
                        caseInsensitive.Name,
                        caseInsensitive.Type,
                        caseInsensitive.Type.CanBeNull()),
                ];
                return true;
            }
        }

        if (!enableNamingConventions)
        {
            segments = [];
            return false;
        }

        // 2. ValueRange<T> property ending in "Range" (e.g., PriceRange -> Price)
        if (dtoProperty.Type.TryGetValueRangeElementType(out _) &&
            propName.Length > "Range".Length &&
            propName.EndsWith("Range", StringComparison.Ordinal))
        {
            var candidate = propName.Substring(0, propName.Length - "Range".Length);
            if (TryResolvePath(compilation, entityType, candidate, out segments, out _))
            {
                return true;
            }
        }

        // 3. Collection property pluralization (e.g., Statuses -> Status, Ids -> Id, Categories -> Category)
        if (dtoProperty.Type.TryGetCollectionElementType(out var elementType) && elementType is not null)
        {
            foreach (var singularCandidate in GetSingularCandidates(propName))
            {
                if (TryResolvePath(compilation, entityType, singularCandidate, out segments, out _) &&
                    !segments[^1].PropertyType.TryGetCollectionElementType(out _) &&
                    elementType.IsScalarCompatibleWith(segments[^1].PropertyType))
                {
                    return true;
                }
            }
        }

        // 4. Prefix conventions: MinAge, StartCreatedAt, MaxPrice, EndCreatedAt
        foreach (var (prefix, opValue) in s_prefixConventions)
        {
            if (propName.Length > prefix.Length &&
                propName.StartsWith(prefix, StringComparison.Ordinal) &&
                char.IsUpper(propName[prefix.Length]))
            {
                var candidate = propName.Substring(prefix.Length);
                if (TryResolvePath(compilation, entityType, candidate, out segments, out _))
                {
                    inferredCompareOperator = opValue;
                    return true;
                }
            }
        }

        // 5. Suffix conventions: AgeFrom, AgeTo, CreatedAtFrom, CreatedAtTo
        foreach (var (suffix, opValue) in s_suffixConventions)
        {
            if (propName.Length > suffix.Length &&
                propName.EndsWith(suffix, StringComparison.Ordinal))
            {
                var candidate = propName.Substring(0, propName.Length - suffix.Length);
                if (TryResolvePath(compilation, entityType, candidate, out segments, out _))
                {
                    inferredCompareOperator = opValue;
                    return true;
                }
            }
        }

        segments = [];
        return false;
    }

    /// <summary>
    /// Determines whether a property on a filter DTO is a built-in control property from <c>ISortable&lt;T&gt;</c> or <c>IPagedFilter</c>.
    /// </summary>
    public static bool IsFrameworkControlProperty(
        IPropertySymbol property,
        bool implementsSortable,
        bool implementsPagedFilter)
    {
        if (implementsSortable && property.Name is "SortBy" or "SortDescending")
        {
            return true;
        }

        if (implementsPagedFilter && property.Name is "Page" or "PageSize")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Searches <paramref name="typeSymbol"/> and its base types/interfaces for a readable instance property.
    /// </summary>
    public static IPropertySymbol? FindAccessibleInstanceProperty(
        Compilation compilation,
        ITypeSymbol typeSymbol,
        string name,
        bool ignoreCase = false)
    {
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        for (var current = typeSymbol as INamedTypeSymbol;
             current is not null && current.SpecialType == SpecialType.None;
             current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol { IsStatic: false, IsIndexer: false, GetMethod: not null } prop &&
                    string.Equals(prop.Name, name, comparison) &&
                    compilation.IsSymbolAccessibleWithin(prop, compilation.Assembly) &&
                    compilation.IsSymbolAccessibleWithin(prop.GetMethod, compilation.Assembly))
                {
                    return prop;
                }
            }
        }

        if (typeSymbol.TypeKind == TypeKind.Interface && typeSymbol is INamedTypeSymbol ifaceType)
        {
            foreach (var iface in new[] { ifaceType }.Concat(ifaceType.AllInterfaces))
            {
                foreach (var member in iface.GetMembers())
                {
                    if (member is IPropertySymbol { IsStatic: false, IsIndexer: false, GetMethod: not null } prop &&
                        string.Equals(prop.Name, name, comparison))
                    {
                        return prop;
                    }
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> GetSingularCandidates(string pluralName)
    {
        if (pluralName.Length > 3 && pluralName.EndsWith("ies", StringComparison.Ordinal))
        {
            yield return pluralName.Substring(0, pluralName.Length - 3) + "y";
        }

        if (pluralName.Length > 2 && pluralName.EndsWith("es", StringComparison.Ordinal))
        {
            yield return pluralName.Substring(0, pluralName.Length - 2);
        }

        if (pluralName.Length > 1 && pluralName.EndsWith("s", StringComparison.Ordinal) &&
            !pluralName.EndsWith("ss", StringComparison.Ordinal))
        {
            yield return pluralName.Substring(0, pluralName.Length - 1);
        }
    }
}

using Microsoft.CodeAnalysis;

namespace Ling.Filter.Helpers;

/// <summary>
/// Represents a single resolved property hop along a dot-separated entity navigation path.
/// </summary>
/// <param name="PropertySymbol">The resolved <see cref="IPropertySymbol"/>.</param>
/// <param name="Name">The C# identifier of the property.</param>
/// <param name="PropertyType">The type of the property.</param>
/// <param name="IsReferenceOrNullableValue">Whether this property hop can evaluate to <see langword="null"/> at runtime.</param>
internal sealed record ResolvedPropertySegment(
    IPropertySymbol PropertySymbol,
    string Name,
    ITypeSymbol PropertyType,
    bool IsReferenceOrNullableValue);

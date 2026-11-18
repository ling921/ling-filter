using System.Collections.Immutable;
using Ling.Filter.Extensions;
using Ling.Filter.Generators.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Ling.Filter.Generators.Models;

/// <summary>
/// Describes the namespace and containing type chain for emitting partial declarations.
/// </summary>
/// <param name="FilenameHint">Safe hint name used for <c>AddSource</c>.</param>
/// <param name="Namespace">Containing namespace (empty string when in the global namespace).</param>
/// <param name="Hierarchy">Containing types ordered from outermost to innermost target type.</param>
internal sealed record HierarchyInfo(
    string FilenameHint,
    string Namespace,
    EquatableArray<TypeInfo> Hierarchy)
{
    public static HierarchyInfo From(INamedTypeSymbol typeSymbol)
    {
        var stack = new Stack<TypeInfo>();
        for (var current = typeSymbol; current is not null; current = current.ContainingType)
        {
            stack.Push(TypeInfo.From(current));
        }

        var ordered = stack.ToImmutableArray();
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();

        var rawHint = typeSymbol.GetFullyQualifiedMetadataName()
            .Replace('+', '.')
            .Replace('`', '_');

        return new HierarchyInfo(rawHint, ns, ordered);
    }
}

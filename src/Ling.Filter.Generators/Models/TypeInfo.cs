using Microsoft.CodeAnalysis;

namespace Ling.Filter.Generators.Models;

/// <summary>
/// Describes a single type declaration header in a nested or top-level partial hierarchy.
/// </summary>
/// <param name="QualifiedName">The type identifier.</param>
/// <param name="DeclarationKeyword">The C# type keyword (<c>class</c>, <c>struct</c>, <c>record</c>, or <c>record struct</c>).</param>
internal sealed record TypeInfo(
    string QualifiedName,
    string DeclarationKeyword)
{
    public static TypeInfo From(INamedTypeSymbol symbol)
    {
        var keyword = (symbol.IsRecord, symbol.IsValueType) switch
        {
            (true, true) => "record struct",
            (true, false) => "record",
            (false, true) => "struct",
            (false, false) => "class",
        };

        return new TypeInfo(symbol.Name, keyword);
    }
}

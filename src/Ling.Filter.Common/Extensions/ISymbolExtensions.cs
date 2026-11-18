using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ling.Filter.Extensions;

/// <summary>
/// Extension methods for <see cref="ISymbol"/> inspection.
/// </summary>
internal static class ISymbolExtensions
{
    private static readonly SymbolDisplayFormat s_fullyQualifiedMetadataFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    /// <summary>
    /// Gets the <c>global::</c>-prefixed fully qualified C# type name for <paramref name="symbol"/>.
    /// </summary>
    public static string GetFullyQualifiedName(this ISymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// Gets the fully qualified metadata name without <c>global::</c> for <paramref name="symbol"/>.
    /// </summary>
    public static string GetFullyQualifiedMetadataName(this ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol { IsGenericType: true } namedType && !SymbolEqualityComparer.Default.Equals(namedType, namedType.ConstructedFrom))
        {
            return namedType.ConstructedFrom.GetFullyQualifiedMetadataName();
        }

        if (symbol is INamedTypeSymbol { Arity: > 0 } genericDefinition)
        {
            return $"{genericDefinition.ToDisplayString(s_fullyQualifiedMetadataFormat)}`{genericDefinition.Arity}";
        }

        return symbol.ToDisplayString(s_fullyQualifiedMetadataFormat);
    }

    /// <summary>
    /// Checks whether <paramref name="symbol"/> represents or references a compiler error type.
    /// </summary>
    public static bool IsErrorType(this ISymbol symbol) => symbol switch
    {
        IErrorTypeSymbol => true,
        IFieldSymbol { Type.TypeKind: TypeKind.Error } => true,
        IPropertySymbol { Type.TypeKind: TypeKind.Error } => true,
        IMethodSymbol { ReturnType.TypeKind: TypeKind.Error } => true,
        IParameterSymbol { Type.TypeKind: TypeKind.Error } => true,
        ITypeSymbol { TypeKind: TypeKind.Error } => true,
        _ => symbol.Kind is SymbolKind.ErrorType,
    };

    /// <summary>
    /// Determines whether <paramref name="typeSymbol"/> and all of its containing types are declared <c>partial</c>
    /// and have zero generic type parameters on containing types.
    /// </summary>
    public static bool IsSupportedPartialHierarchy(this INamedTypeSymbol typeSymbol, out string? failureReason)
    {
        if (typeSymbol.Arity > 0)
        {
            failureReason = $"type '{typeSymbol.Name}' cannot have generic type parameters";
            return false;
        }

        for (var current = typeSymbol; current is not null; current = current.ContainingType)
        {
            if (current.Arity > 0)
            {
                failureReason = $"containing type '{current.Name}' cannot have generic type parameters";
                return false;
            }

            var hasPartialDeclaration = current.DeclaringSyntaxReferences.Any(static syntaxRef =>
                syntaxRef.GetSyntax() is TypeDeclarationSyntax typeSyntax &&
                typeSyntax.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)));

            if (!hasPartialDeclaration)
            {
                failureReason = ReferenceEquals(current, typeSymbol)
                    ? $"type '{typeSymbol.Name}' must be declared partial"
                    : $"containing type '{current.Name}' must be declared partial";
                return false;
            }
        }

        failureReason = null;
        return true;
    }
}

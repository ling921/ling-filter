using System.Collections.Immutable;
using Ling.Filter.Analyzers.Diagnostics;
using Ling.Filter.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ling.Filter.Analyzers;

/// <summary>
/// Analyzes methods marked with <c>[CustomFilter]</c> to ensure they meet the signature contract (<c>LF0006</c>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CustomFilterMethodAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        DiagnosticDescriptors.InvalidCustomFilterMethod,
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodSyntax || methodSyntax.AttributeLists.Count == 0)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(methodSyntax, context.CancellationToken) is not IMethodSymbol methodSymbol ||
            methodSymbol.IsErrorType() ||
            methodSymbol.ContainingType is not { } containingType)
        {
            return;
        }

        var hasCustomFilter = methodSymbol.GetAttributes().Any(static a =>
            a.AttributeClass?.GetFullyQualifiedMetadataName() == Constants.CustomFilterAttributeMetadataName);

        if (!hasCustomFilter)
        {
            return;
        }

        var location = methodSyntax.Identifier.GetLocation();
        var filterInterfaces = containingType.GetImplementedGenericInterfaces(Constants.IFilterableMetadataName).ToArray();
        if (filterInterfaces.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidCustomFilterMethod,
                location,
                methodSymbol.Name,
                "must be declared inside a type implementing IFilterable<TEntity>"));
            return;
        }

        if (methodSymbol.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidCustomFilterMethod,
                location,
                methodSymbol.Name,
                "cannot be static"));
            return;
        }

        if (methodSymbol.IsGenericMethod || methodSymbol.Parameters.Length > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidCustomFilterMethod,
                location,
                methodSymbol.Name,
                "must be non-generic and take zero parameters"));
            return;
        }

        if (!IsValidExpressionReturnType(methodSymbol.ReturnType, filterInterfaces))
        {
            var expectedTypes = string.Join(
                " or ",
                filterInterfaces.Select(static i => $"Expression<Func<{i.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}, bool>>?"));

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidCustomFilterMethod,
                location,
                methodSymbol.Name,
                $"must return {expectedTypes}"));
        }
    }

    private static bool IsValidExpressionReturnType(ITypeSymbol returnType, INamedTypeSymbol[] filterInterfaces)
    {
        if (returnType is not INamedTypeSymbol { TypeArguments.Length: 1 } exprType ||
            exprType.GetFullyQualifiedMetadataName() != "System.Linq.Expressions.Expression`1")
        {
            return false;
        }

        if (exprType.TypeArguments[0] is not INamedTypeSymbol { TypeArguments.Length: 2 } funcType ||
            funcType.GetFullyQualifiedMetadataName() != "System.Func`2" ||
            funcType.TypeArguments[1].SpecialType != SpecialType.System_Boolean)
        {
            return false;
        }

        var entityArg = funcType.TypeArguments[0];
        return filterInterfaces.Any(iface => SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], entityArg));
    }
}

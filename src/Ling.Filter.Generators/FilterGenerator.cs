using Ling.Filter.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ling.Filter.Generators;

/// <summary>
/// Roslyn incremental source generator that synthesizes <c>IFilterable&lt;TEntity&gt;</c> and <c>ISortable&lt;TEntity&gt;</c>
/// implementations for partial filter models.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed partial class FilterGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var specs = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateSyntax(node),
                transform: static (ctx, ct) => Parser.TryParse(ctx, ct))
            .Where(static spec => spec is not null);

        context.RegisterSourceOutput(specs, static (spc, spec) =>
        {
            if (spec is null)
            {
                return;
            }

            var sourceText = Execute.GenerateSource(spec);
            spc.AddSource($"{spec.Hierarchy.FilenameHint}.Filter.g.cs", sourceText);
        });
    }

    private static bool IsCandidateSyntax(SyntaxNode node)
    {
        return node is TypeDeclarationSyntax
        {
            BaseList.Types.Count: > 0,
        } typeDecl
        && typeDecl is not InterfaceDeclarationSyntax
        && typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword);
    }
}

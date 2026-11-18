using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ling.Filter.CodeFixes;

/// <summary>
/// Provides a Roslyn code fix for <c>LF0001</c> that adds the <see langword="partial"/> modifier
/// to the target filter/sortable type declaration and any non-partial containing types.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddPartialModifierCodeFixProvider)), Shared]
public sealed class AddPartialModifierCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = "Ling.Filter.AddPartialModifier";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
    [
        DiagnosticIds.FilterTypeMustBePartialId,
    ];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan);
        if (node.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { } typeDeclaration)
        {
            return;
        }

        // Do not offer a partial-modifier fix if the failure is due to generic type parameters
        if (typeDeclaration.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Any(static t => t.TypeParameterList is not null))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add 'partial' modifier",
                createChangedDocument: ct => AddPartialModifiersAsync(context.Document, typeDeclaration, ct),
                equivalenceKey: EquivalenceKey),
            diagnostic);
    }

    private static async Task<Document> AddPartialModifiersAsync(
        Document document,
        TypeDeclarationSyntax targetType,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var typesToUpdate = targetType
            .AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .Where(static t => !t.Modifiers.Any(SyntaxKind.PartialKeyword))
            .ToArray();

        var newRoot = root.ReplaceNodes(
            typesToUpdate,
            static (_, rewritten) => rewritten.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword)));

        return document.WithSyntaxRoot(newRoot);
    }
}

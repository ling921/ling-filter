using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ling.Filter.CodeFixes;

/// <summary>
/// Provides a Roslyn code fix for <c>LF0005</c> that decorates an unmapped property on a filter model
/// with <c>[FilterIgnore]</c> (adding <c>using Ling.Filter.Annotations;</c> if not already imported).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddFilterIgnoreAttributeCodeFixProvider)), Shared]
public sealed class AddFilterIgnoreAttributeCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = "Ling.Filter.AddFilterIgnoreAttribute";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
    [
        DiagnosticIds.UnmappedFilterPropertyId,
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
        if (node.FirstAncestorOrSelf<PropertyDeclarationSyntax>() is not { } propDeclaration)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add [FilterIgnore] attribute",
                createChangedDocument: ct => AddFilterIgnoreAsync(context.Document, propDeclaration, ct),
                equivalenceKey: EquivalenceKey),
            diagnostic);
    }

    private static async Task<Document> AddFilterIgnoreAsync(
        Document document,
        PropertyDeclarationSyntax propertyDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return document;
        }

        var eol = compilationUnit
            .DescendantTrivia()
            .FirstOrDefault(static t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        if (eol == default)
        {
            eol = SyntaxFactory.CarriageReturnLineFeed;
        }

        var leadingTrivia = propertyDeclaration.GetLeadingTrivia();
        var indentation = leadingTrivia.LastOrDefault(static t => t.IsKind(SyntaxKind.WhitespaceTrivia));
        var propIndentList = indentation == default
            ? SyntaxTriviaList.Empty
            : SyntaxFactory.TriviaList(indentation);

        var attrNode = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("FilterIgnore"))));

        PropertyDeclarationSyntax updatedProperty;
        if (propertyDeclaration.AttributeLists.Count == 0)
        {
            var positionedAttr = attrNode
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(eol);

            updatedProperty = propertyDeclaration
                .WithLeadingTrivia(propIndentList)
                .WithAttributeLists(SyntaxFactory.SingletonList(positionedAttr));
        }
        else
        {
            var positionedAttr = attrNode
                .WithLeadingTrivia(propIndentList)
                .WithTrailingTrivia(eol);

            updatedProperty = propertyDeclaration.AddAttributeLists(positionedAttr);
        }

        var newCompilationUnit = compilationUnit.ReplaceNode(propertyDeclaration, updatedProperty);

        var hasAnnotationsUsing = newCompilationUnit.Usings.Any(static u =>
            u.Name?.ToString() == Constants.AnnotationsNamespace);

        if (!hasAnnotationsUsing)
        {
            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName(Constants.AnnotationsNamespace)
                    .WithLeadingTrivia(SyntaxFactory.Space));

            if (newCompilationUnit.Usings.Count > 0)
            {
                var lastUsing = newCompilationUnit.Usings[^1];
                usingDirective = usingDirective.WithTrailingTrivia(lastUsing.GetTrailingTrivia());
                newCompilationUnit = newCompilationUnit.WithUsings(newCompilationUnit.Usings.Add(usingDirective));
            }
            else
            {
                usingDirective = usingDirective.WithTrailingTrivia(eol, eol);
                newCompilationUnit = newCompilationUnit.WithUsings(SyntaxFactory.SingletonList(usingDirective));
            }
        }

        return document.WithSyntaxRoot(newCompilationUnit);
    }
}

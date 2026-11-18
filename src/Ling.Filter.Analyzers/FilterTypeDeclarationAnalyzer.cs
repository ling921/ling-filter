using System.Collections.Immutable;
using Ling.Filter.Analyzers.Diagnostics;
using Ling.Filter.Extensions;
using Ling.Filter.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ling.Filter.Analyzers;

/// <summary>
/// Analyzes filter and sortable type declarations for partial modifier requirements (<c>LF0001</c>),
/// class-level sort attribute targets (<c>LF0002</c>), and unmapped convention properties (<c>LF0004</c>, <c>LF0005</c>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FilterTypeDeclarationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        DiagnosticDescriptors.FilterTypeMustBePartial,
        DiagnosticDescriptors.TargetPropertyPathNotFound,
        DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
        DiagnosticDescriptors.UnmappedFilterProperty,
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeTypeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not TypeDeclarationSyntax typeSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(typeSyntax, context.CancellationToken) is not INamedTypeSymbol typeSymbol ||
            typeSymbol.IsErrorType() ||
            typeSymbol.TypeKind == TypeKind.Interface)
        {
            return;
        }

        var filterInterfaces = typeSymbol.GetImplementedGenericInterfaces(Constants.IFilterableMetadataName).ToArray();
        var sortInterfaces = typeSymbol.GetImplementedGenericInterfaces(Constants.ISortableMetadataName).ToArray();
        if (filterInterfaces.Length == 0 && sortInterfaces.Length == 0)
        {
            return;
        }

        var isPrimaryDeclaration = typeSymbol.Locations.Length > 0 &&
                                   ReferenceEquals(typeSyntax.SyntaxTree, typeSymbol.Locations[0].SourceTree) &&
                                   typeSyntax.Identifier.Span == typeSymbol.Locations[0].SourceSpan;

        if (isPrimaryDeclaration && !typeSymbol.IsSupportedPartialHierarchy(out var reason))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FilterTypeMustBePartial,
                typeSyntax.Identifier.GetLocation(),
                reason));
        }

        if (isPrimaryDeclaration)
        {
            AnalyzeClassLevelSortAttributes(context, typeSymbol, sortInterfaces);
        }

        if (filterInterfaces.Length == 0)
        {
            return;
        }

        // Read [FilterOptions]
        var autoMapSameName = true;
        var enableNamingConventions = true;
        var defaultStringMode = 0; // StringFilterMode.Equals
        var warnOnUnmapped = true;

        foreach (var attr in typeSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.GetFullyQualifiedMetadataName() == Constants.FilterOptionsAttributeMetadataName)
            {
                autoMapSameName = attr.GetNamedValue("AutoMapSameNameProperties", true);
                enableNamingConventions = attr.GetNamedValue("EnableNamingConventions", true);
                defaultStringMode = attr.GetEnumIntValue("DefaultStringMode", 0);
                warnOnUnmapped = attr.GetNamedValue("WarnOnUnmappedProperties", true);
                break;
            }
        }

        var implementsSortable = sortInterfaces.Length > 0;
        var implementsPaged = typeSymbol.AllInterfaces.Any(static i => i.GetFullyQualifiedMetadataName() == Constants.IPagedFilterMetadataName);

        // Inspect properties declared in THIS partial declaration syntax
        foreach (var memberSyntax in typeSyntax.Members)
        {
            if (memberSyntax is not PropertyDeclarationSyntax propSyntax)
            {
                continue;
            }

            if (context.SemanticModel.GetDeclaredSymbol(propSyntax, context.CancellationToken) is not IPropertySymbol propSymbol ||
                propSymbol.IsErrorType() ||
                propSymbol.IsStatic ||
                propSymbol.IsIndexer ||
                propSymbol.GetMethod is null ||
                propSymbol.DeclaredAccessibility != Accessibility.Public ||
                propSymbol.ExplicitInterfaceImplementations.Length > 0)
            {
                continue;
            }

            var propAttrs = propSymbol.GetAttributes();
            var hasIgnoreOrExplicit = propAttrs.Any(static a =>
            {
                var name = a.AttributeClass?.GetFullyQualifiedMetadataName();
                return name is not null &&
                       (name == Constants.FilterIgnoreAttributeMetadataName ||
                        name == Constants.SortFieldAttributeMetadataName ||
                        Constants.PrimaryFilterAttributeNames.Contains(name));
            });

            if (hasIgnoreOrExplicit || PropertyPathResolver.IsFrameworkControlProperty(propSymbol, implementsSortable, implementsPaged))
            {
                continue;
            }

            foreach (var filterIface in filterInterfaces)
            {
                var entityType = filterIface.TypeArguments[0];
                if (entityType.IsErrorType())
                {
                    continue;
                }

                if (!PropertyPathResolver.TryResolveConventionTarget(
                        context.Compilation,
                        entityType,
                        propSymbol,
                        autoMapSameName,
                        enableNamingConventions,
                        out var segments,
                        out var inferredOperator))
                {
                    if (warnOnUnmapped && (autoMapSameName || enableNamingConventions))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.UnmappedFilterProperty,
                            propSyntax.Identifier.GetLocation(),
                            propSymbol.Name,
                            entityType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                    }

                    continue;
                }

                ValidateConventionCompatibility(
                    context,
                    propSyntax.Identifier.GetLocation(),
                    propSymbol,
                    segments,
                    inferredOperator,
                    defaultStringMode);
            }
        }
    }

    private static void AnalyzeClassLevelSortAttributes(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol[] sortInterfaces)
    {
        if (sortInterfaces.Length == 0)
        {
            return;
        }

        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.GetFullyQualifiedMetadataName();
            string? targetPath = null;

            if (attrName == Constants.DefaultSortAttributeMetadataName)
            {
                targetPath = attr.GetStringValue("Target", fallbackCtorIndex: 0);
            }
            else if (attrName == Constants.SortFieldAttributeMetadataName)
            {
                var keyName = attr.GetStringValue("Name", fallbackCtorIndex: 0);
                targetPath = attr.GetStringValue("Target", fallbackCtorIndex: 1) ?? keyName;
            }

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                continue;
            }

            foreach (var sortIface in sortInterfaces)
            {
                var entityType = sortIface.TypeArguments[0];
                if (!entityType.IsErrorType() &&
                    !PropertyPathResolver.TryResolvePath(context.Compilation, entityType, targetPath!, out _, out var failed))
                {
                    var location = attr.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                                   ?? typeSymbol.Locations.FirstOrDefault()
                                   ?? Location.None;
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.TargetPropertyPathNotFound,
                        location,
                        targetPath,
                        entityType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        failed ?? targetPath));
                }
            }
        }
    }

    private static void ValidateConventionCompatibility(
        SyntaxNodeAnalysisContext context,
        Location location,
        IPropertySymbol propSymbol,
        ImmutableArray<ResolvedPropertySegment> segments,
        int? inferredOperator,
        int defaultStringMode)
    {
        var leaf = segments[^1];
        var targetPath = string.Join(".", segments.Select(static s => s.Name));
        var targetType = leaf.PropertyType;

        if (propSymbol.Type.TryGetValueRangeElementType(out var rangeElem) && rangeElem is not null)
        {
            if (!rangeElem.IsScalarCompatibleWith(targetType) || !targetType.SupportsRelationalOperators())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                    location,
                    propSymbol.Name,
                    propSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    targetPath,
                    targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    "ValueRange element type must match a comparable target property"));
            }

            return;
        }

        if (propSymbol.Type.TryGetCollectionElementType(out var collElem) && collElem is not null &&
            !targetType.TryGetCollectionElementType(out _))
        {
            if (!collElem.IsScalarCompatibleWith(targetType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                    location,
                    propSymbol.Name,
                    propSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    targetPath,
                    targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    "collection element type is incompatible with the target scalar property"));
            }

            return;
        }

        if (propSymbol.Type.IsStringType())
        {
            if (!targetType.IsStringType())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                    location,
                    propSymbol.Name,
                    "string",
                    targetPath,
                    targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    defaultStringMode is 0 or 1
                        ? "string property cannot be compared to a non-string target"
                        : "string matching requires a string target property"));
            }

            return;
        }

        if (!propSymbol.Type.IsScalarCompatibleWith(targetType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                location,
                propSymbol.Name,
                propSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                targetPath,
                targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                "types are not comparable"));
            return;
        }

        if (inferredOperator is 2 or 3 or 4 or 5 && !targetType.SupportsRelationalOperators())
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                location,
                propSymbol.Name,
                propSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                targetPath,
                targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                "target property does not support relational (<, <=, >, >=) comparison"));
        }
    }
}

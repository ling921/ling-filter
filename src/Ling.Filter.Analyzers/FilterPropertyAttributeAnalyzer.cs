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
/// Analyzes property-level filter and sort attributes on types implementing <c>IFilterable&lt;TEntity&gt;</c> or <c>ISortable&lt;TEntity&gt;</c>,
/// reporting <c>LF0002</c>, <c>LF0003</c>, <c>LF0004</c>, <c>LF0007</c>, <c>LF0008</c>, and <c>LF0009</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FilterPropertyAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        DiagnosticDescriptors.TargetPropertyPathNotFound,
        DiagnosticDescriptors.AttributeIncompatibleWithPropertyType,
        DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
        DiagnosticDescriptors.InvalidCollectionFilterTarget,
        DiagnosticDescriptors.ConflictingFilterAttributes,
        DiagnosticDescriptors.EmptyMultiFieldFilter,
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not PropertyDeclarationSyntax propSyntax || propSyntax.AttributeLists.Count == 0)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(propSyntax, context.CancellationToken) is not IPropertySymbol propSymbol ||
            propSymbol.IsErrorType() ||
            propSymbol.ContainingType is not { } containingType)
        {
            return;
        }

        var filterInterfaces = containingType.GetImplementedGenericInterfaces(Constants.IFilterableMetadataName).ToArray();
        var sortInterfaces = containingType.GetImplementedGenericInterfaces(Constants.ISortableMetadataName).ToArray();
        if (filterInterfaces.Length == 0 && sortInterfaces.Length == 0)
        {
            return;
        }

        var attributes = propSymbol.GetAttributes();
        var hasFilterIgnore = false;
        AttributeData? filterGroupAttr = null;
        var primaryAttrs = new List<(string MetadataName, AttributeData Data)>();

        foreach (var attr in attributes)
        {
            var metaName = attr.AttributeClass?.GetFullyQualifiedMetadataName();
            if (metaName is null)
            {
                continue;
            }

            if (metaName == Constants.FilterIgnoreAttributeMetadataName)
            {
                hasFilterIgnore = true;
            }
            else if (metaName == Constants.FilterGroupAttributeMetadataName)
            {
                filterGroupAttr = attr;
            }
            else if (Constants.PrimaryFilterAttributeNames.Contains(metaName))
            {
                primaryAttrs.Add((metaName, attr));
            }
            else if (metaName == Constants.SortFieldAttributeMetadataName)
            {
                ValidatePropertySortAttribute(context, propSymbol, attr, sortInterfaces);
            }
        }

        // Check LF0008: conflicting attributes
        if (hasFilterIgnore && (primaryAttrs.Count > 0 || filterGroupAttr is not null))
        {
            var conflictNames = primaryAttrs.Select(static p => p.Data.AttributeClass!.Name).ToList();
            if (filterGroupAttr is not null)
            {
                conflictNames.Add(filterGroupAttr.AttributeClass!.Name);
            }

            conflictNames.Insert(0, "FilterIgnoreAttribute");
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ConflictingFilterAttributes,
                propSyntax.Identifier.GetLocation(),
                propSymbol.Name,
                string.Join(", ", conflictNames)));
            return;
        }

        if (primaryAttrs.Count > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ConflictingFilterAttributes,
                propSyntax.Identifier.GetLocation(),
                propSymbol.Name,
                string.Join(", ", primaryAttrs.Select(static p => p.Data.AttributeClass!.Name))));
            return;
        }

        if (primaryAttrs.Count == 0 || filterInterfaces.Length == 0)
        {
            return;
        }

        var (attrMetaName, attrData) = primaryAttrs[0];
        var attrLocation = attrData.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                           ?? propSyntax.Identifier.GetLocation();

        // Check LF0003: DTO property type compatibility with attribute
        if (!ValidateDtoPropertyTypeForAttribute(context, attrLocation, propSymbol, attrMetaName, attrData))
        {
            return;
        }

        // Check against each implemented IFilterable<TEntity>
        foreach (var filterIface in filterInterfaces)
        {
            var entityType = filterIface.TypeArguments[0];
            if (entityType.IsErrorType())
            {
                continue;
            }

            if (attrMetaName == Constants.MultiFieldFilterAttributeMetadataName)
            {
                ValidateMultiFieldAttribute(context, attrLocation, propSymbol, attrData, entityType);
            }
            else if (attrMetaName == Constants.CollectionFilterAttributeMetadataName)
            {
                ValidateCollectionFilterAttribute(context, attrLocation, propSymbol, attrData, entityType);
            }
            else
            {
                ValidateSingleTargetAttribute(context, attrLocation, propSymbol, attrMetaName, attrData, entityType);
            }
        }
    }

    private static void ValidatePropertySortAttribute(
        SyntaxNodeAnalysisContext context,
        IPropertySymbol propSymbol,
        AttributeData attr,
        INamedTypeSymbol[] sortInterfaces)
    {
        var keyName = attr.GetStringValue("Name", fallbackCtorIndex: 0);
        var targetPath = attr.GetStringValue("Target", fallbackCtorIndex: 1) ?? keyName ?? propSymbol.Name;
        var location = attr.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                       ?? propSymbol.Locations.FirstOrDefault()
                       ?? Location.None;

        foreach (var sortIface in sortInterfaces)
        {
            var entityType = sortIface.TypeArguments[0];
            if (!entityType.IsErrorType() &&
                !PropertyPathResolver.TryResolvePath(context.Compilation, entityType, targetPath, out _, out var failed))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TargetPropertyPathNotFound,
                    location,
                    targetPath,
                    entityType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    failed ?? targetPath));
            }
        }
    }

    private static bool ValidateDtoPropertyTypeForAttribute(
        SyntaxNodeAnalysisContext context,
        Location location,
        IPropertySymbol propSymbol,
        string attrMetaName,
        AttributeData attrData)
    {
        var propType = propSymbol.Type;
        var attrShortName = attrData.AttributeClass?.Name ?? attrMetaName;
        var typeDisplay = propType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        switch (attrMetaName)
        {
            case Constants.StringFilterAttributeMetadataName:
                if (!propType.IsStringType())
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.AttributeIncompatibleWithPropertyType,
                        location,
                        attrShortName,
                        propSymbol.Name,
                        typeDisplay,
                        "expected 'string'"));
                    return false;
                }

                return true;

            case Constants.RangeFilterAttributeMetadataName:
                if (!propType.TryGetValueRangeElementType(out var rangeElem) || rangeElem is null || !rangeElem.SupportsRelationalOperators())
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.AttributeIncompatibleWithPropertyType,
                        location,
                        attrShortName,
                        propSymbol.Name,
                        typeDisplay,
                        "expected 'ValueRange<T>' where T is a comparable value type"));
                    return false;
                }

                if (attrData.GetEnumIntValue("Precision", defaultValue: 0) != 0 &&
                    !rangeElem.IsDateOrTimeType(out _))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.AttributeIncompatibleWithPropertyType,
                        location,
                        attrShortName,
                        propSymbol.Name,
                        typeDisplay,
                        "DatePrecision requires 'ValueRange<T>' where T is 'DateTime', 'DateTimeOffset', 'DateOnly', or 'TimeOnly'"));
                    return false;
                }

                return true;

            case Constants.DateFilterAttributeMetadataName:
                if (!propType.IsDateOrTimeType(out _))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.AttributeIncompatibleWithPropertyType,
                        location,
                        attrShortName,
                        propSymbol.Name,
                        typeDisplay,
                        "expected 'DateTime', 'DateTimeOffset', 'DateOnly', or 'TimeOnly'"));
                    return false;
                }

                return true;

            case Constants.NullCheckFilterAttributeMetadataName:
                if (!propType.IsBooleanType())
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.AttributeIncompatibleWithPropertyType,
                        location,
                        attrShortName,
                        propSymbol.Name,
                        typeDisplay,
                        "expected 'bool' or 'bool?'"));
                    return false;
                }

                return true;

            case Constants.FlagsFilterAttributeMetadataName:
                if (!propType.IsEnumType(out _))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.AttributeIncompatibleWithPropertyType,
                        location,
                        attrShortName,
                        propSymbol.Name,
                        typeDisplay,
                        "expected an enum or nullable enum type"));
                    return false;
                }

                return true;

            case Constants.InFilterAttributeMetadataName:
            case Constants.NotInFilterAttributeMetadataName:
                if (!propType.TryGetCollectionElementType(out _))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.AttributeIncompatibleWithPropertyType,
                        location,
                        attrShortName,
                        propSymbol.Name,
                        typeDisplay,
                        "expected an array or 'IEnumerable<T>' collection"));
                    return false;
                }

                return true;

            case Constants.CompareFilterAttributeMetadataName:
            {
                var op = attrData.GetEnumIntValue("Operator", defaultValue: 0, ctorIndex: 0);
                if (op is 2 or 3 or 4 or 5 && !propType.SupportsRelationalOperators())
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.AttributeIncompatibleWithPropertyType,
                        location,
                        attrShortName,
                        propSymbol.Name,
                        typeDisplay,
                        "relational operators (>, >=, <, <=) require a numeric, date, enum, or comparable type"));
                    return false;
                }

                return true;
            }

            default:
                return true;
        }
    }

    private static void ValidateMultiFieldAttribute(
        SyntaxNodeAnalysisContext context,
        Location location,
        IPropertySymbol propSymbol,
        AttributeData attrData,
        ITypeSymbol entityType)
    {
        var fields = ExtractMultiFieldNames(attrData);
        if (fields.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.EmptyMultiFieldFilter,
                location,
                propSymbol.Name));
            return;
        }

        var stringMode = attrData.GetEnumIntValue("Mode", defaultValue: 2); // Contains
        var op = attrData.GetEnumIntValue("Operator", defaultValue: 0);
        foreach (var fieldPath in fields)
        {
            if (!PropertyPathResolver.TryResolvePath(context.Compilation, entityType, fieldPath, out var segments, out var failed))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TargetPropertyPathNotFound,
                    location,
                    fieldPath,
                    entityType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    failed ?? fieldPath));
                continue;
            }

            var targetType = segments[^1].PropertyType;
            if (propSymbol.Type.IsStringType())
            {
                if (!targetType.IsStringType())
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                        location,
                        propSymbol.Name,
                        "string",
                        fieldPath,
                        targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        stringMode is 0 or 1
                            ? "target field must be string"
                            : "string pattern matching requires a string target field"));
                }
            }
            else if (!propSymbol.Type.IsScalarCompatibleWith(targetType) ||
                     (op is 2 or 3 or 4 or 5 && (!propSymbol.Type.SupportsRelationalOperators() || !targetType.SupportsRelationalOperators())))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                    location,
                    propSymbol.Name,
                    propSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    fieldPath,
                    targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    "target field type is incompatible"));
            }
        }
    }

    private static void ValidateCollectionFilterAttribute(
        SyntaxNodeAnalysisContext context,
        Location location,
        IPropertySymbol propSymbol,
        AttributeData attrData,
        ITypeSymbol entityType)
    {
        attrData.GetCollectionFilterArgs(out var navPath, out _, out var elementPath);
        if (string.IsNullOrWhiteSpace(navPath))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidCollectionFilterTarget,
                location,
                propSymbol.Name,
                "NavigationProperty must not be empty"));
            return;
        }

        if (!PropertyPathResolver.TryResolvePath(context.Compilation, entityType, navPath!, out var navSegments, out var failedNav))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TargetPropertyPathNotFound,
                location,
                navPath,
                entityType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                failedNav ?? navPath));
            return;
        }

        var navType = navSegments[^1].PropertyType;
        if (!navType.TryGetCollectionElementType(out var elementType) || elementType is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidCollectionFilterTarget,
                location,
                propSymbol.Name,
                $"navigation property '{navPath}' of type '{navType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}' is not a collection"));
            return;
        }

        ITypeSymbol targetScalarType;
        string displayTarget;

        if (!string.IsNullOrWhiteSpace(elementPath))
        {
            if (!PropertyPathResolver.TryResolvePath(context.Compilation, elementType, elementPath!, out var elemSegments, out var failedElem))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidCollectionFilterTarget,
                    location,
                    propSymbol.Name,
                    $"element property path '{elementPath}' was not found on element type '{elementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}' (missing '{failedElem ?? elementPath}')"));
                return;
            }

            targetScalarType = elemSegments[^1].PropertyType;
            displayTarget = $"{navPath}[].{elementPath}";
        }
        else
        {
            targetScalarType = elementType;
            displayTarget = $"{navPath}[]";
        }

        // DTO property can be a collection (In-check against element target) or a scalar/ValueRange
        if (propSymbol.Type.TryGetCollectionElementType(out var dtoCollElem) && dtoCollElem is not null)
        {
            if (!dtoCollElem.IsScalarCompatibleWith(targetScalarType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                    location,
                    propSymbol.Name,
                    propSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    displayTarget,
                    targetScalarType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    "collection element types are incompatible"));
            }

            return;
        }

        if (propSymbol.Type.IsStringType())
        {
            if (!targetScalarType.IsStringType())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                    location,
                    propSymbol.Name,
                    "string",
                    displayTarget,
                    targetScalarType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    "string filter cannot target a non-string collection element member"));
            }

            return;
        }

        var op = attrData.GetEnumIntValue("Operator", defaultValue: 0);
        if (!propSymbol.Type.IsScalarCompatibleWith(targetScalarType) ||
            (op is 2 or 3 or 4 or 5 && !targetScalarType.SupportsRelationalOperators()))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                location,
                propSymbol.Name,
                propSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                displayTarget,
                targetScalarType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                "incompatible collection element comparison"));
        }
    }

    private static void ValidateSingleTargetAttribute(
        SyntaxNodeAnalysisContext context,
        Location location,
        IPropertySymbol propSymbol,
        string attrMetaName,
        AttributeData attrData,
        ITypeSymbol entityType)
    {
        var explicitTarget = attrData.GetTargetPropertyPath();
        ImmutableArray<ResolvedPropertySegment> segments;

        if (!string.IsNullOrWhiteSpace(explicitTarget))
        {
            if (!PropertyPathResolver.TryResolvePath(context.Compilation, entityType, explicitTarget!, out segments, out var failed))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TargetPropertyPathNotFound,
                    location,
                    explicitTarget,
                    entityType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    failed ?? explicitTarget));
                return;
            }
        }
        else
        {
            if (!PropertyPathResolver.TryResolveConventionTarget(
                    context.Compilation,
                    entityType,
                    propSymbol,
                    autoMapSameName: true,
                    enableNamingConventions: true,
                    out segments,
                    out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TargetPropertyPathNotFound,
                    location,
                    propSymbol.Name,
                    entityType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    propSymbol.Name));
                return;
            }
        }

        var leaf = segments[^1];
        var targetType = leaf.PropertyType;
        var targetDisplay = string.Join(".", segments.Select(static s => s.Name));
        var propTypeDisplay = propSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var targetTypeDisplay = targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        switch (attrMetaName)
        {
            case Constants.NullCheckFilterAttributeMetadataName:
                if (!leaf.IsReferenceOrNullableValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                        location,
                        propSymbol.Name,
                        propTypeDisplay,
                        targetDisplay,
                        targetTypeDisplay,
                        "NullCheckFilter target must be a reference type or Nullable<T>"));
                }

                break;

            case Constants.StringFilterAttributeMetadataName:
                if (!targetType.IsStringType())
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                        location,
                        propSymbol.Name,
                        propTypeDisplay,
                        targetDisplay,
                        targetTypeDisplay,
                        "StringFilter target must be of type 'string'"));
                }

                break;

            case Constants.RangeFilterAttributeMetadataName:
                if (propSymbol.Type.TryGetValueRangeElementType(out var rangeElem) &&
                    rangeElem is not null &&
                    (!rangeElem.IsScalarCompatibleWith(targetType) || !targetType.SupportsRelationalOperators()))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                        location,
                        propSymbol.Name,
                        propTypeDisplay,
                        targetDisplay,
                        targetTypeDisplay,
                        "ValueRange<T> element type must be compatible with a comparable target property"));
                }

                break;

            case Constants.DateFilterAttributeMetadataName:
                if (!targetType.IsDateOrTimeType(out var targetDateKind) ||
                    !propSymbol.Type.IsDateOrTimeType(out var propDateKind) ||
                    targetDateKind != propDateKind)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                        location,
                        propSymbol.Name,
                        propTypeDisplay,
                        targetDisplay,
                        targetTypeDisplay,
                        "DateFilter property and target must share the same date/time type"));
                }

                break;

            case Constants.FlagsFilterAttributeMetadataName:
                if (!propSymbol.Type.IsEnumType(out var propEnum) ||
                    !targetType.IsEnumType(out var targetEnum) ||
                    !SymbolEqualityComparer.Default.Equals(propEnum, targetEnum))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                        location,
                        propSymbol.Name,
                        propTypeDisplay,
                        targetDisplay,
                        targetTypeDisplay,
                        "FlagsFilter property and target must share the same enum type"));
                }

                break;

            case Constants.InFilterAttributeMetadataName:
            case Constants.NotInFilterAttributeMetadataName:
                if (propSymbol.Type.TryGetCollectionElementType(out var collElem) &&
                    collElem is not null &&
                    !collElem.IsScalarCompatibleWith(targetType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                        location,
                        propSymbol.Name,
                        propTypeDisplay,
                        targetDisplay,
                        targetTypeDisplay,
                        "collection element type must be compatible with the target scalar property"));
                }

                break;

            case Constants.CompareFilterAttributeMetadataName:
            case Constants.FilterFieldAttributeMetadataName:
            {
                if (propSymbol.Type.TryGetValueRangeElementType(out var fieldRangeElem) && fieldRangeElem is not null)
                {
                    if (!fieldRangeElem.IsScalarCompatibleWith(targetType) || !targetType.SupportsRelationalOperators())
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                            location,
                            propSymbol.Name,
                            propTypeDisplay,
                            targetDisplay,
                            targetTypeDisplay,
                            "ValueRange<T> element type must match a comparable target property"));
                    }

                    break;
                }

                if (propSymbol.Type.TryGetCollectionElementType(out var fieldCollElem) &&
                    fieldCollElem is not null &&
                    !targetType.TryGetCollectionElementType(out _))
                {
                    if (!fieldCollElem.IsScalarCompatibleWith(targetType))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                            location,
                            propSymbol.Name,
                            propTypeDisplay,
                            targetDisplay,
                            targetTypeDisplay,
                            "collection element type must match the target scalar property"));
                    }

                    break;
                }

                var op = attrMetaName == Constants.CompareFilterAttributeMetadataName
                    ? attrData.GetEnumIntValue("Operator", defaultValue: 0, ctorIndex: 0)
                    : 0;

                if (!propSymbol.Type.IsScalarCompatibleWith(targetType) ||
                    (op is 2 or 3 or 4 or 5 && !targetType.SupportsRelationalOperators()))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.FilterAndTargetTypesIncompatible,
                        location,
                        propSymbol.Name,
                        propTypeDisplay,
                        targetDisplay,
                        targetTypeDisplay,
                        "filter and target types are incompatible for the requested comparison"));
                }

                break;
            }
        }
    }

    private static ImmutableArray<string> ExtractMultiFieldNames(AttributeData attrData)
    {
        if (attrData.ConstructorArguments.Length == 0)
        {
            return [];
        }

        var arg = attrData.ConstructorArguments[0];
        if (arg.Kind != TypedConstantKind.Array || arg.Values.IsDefaultOrEmpty)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<string>(arg.Values.Length);
        foreach (var item in arg.Values)
        {
            if (item.Value is string field && !string.IsNullOrWhiteSpace(field))
            {
                builder.Add(field);
            }
        }

        return builder.ToImmutable();
    }
}

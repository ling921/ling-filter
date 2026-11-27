using System.Collections.Immutable;
using Ling.Filter.Extensions;
using Ling.Filter.Generators.Infrastructure;
using Ling.Filter.Generators.Models;
using Ling.Filter.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ling.Filter.Generators;

partial class FilterGenerator
{
    private static class Parser
    {
        public static FilterGenerationSpec? TryParse(GeneratorSyntaxContext context, CancellationToken ct)
        {
            if (context.Node is not TypeDeclarationSyntax typeSyntax)
            {
                return null;
            }

            if (context.SemanticModel.GetDeclaredSymbol(typeSyntax, ct) is not INamedTypeSymbol typeSymbol ||
                typeSymbol.IsErrorType() ||
                typeSymbol.TypeKind == TypeKind.Interface)
            {
                return null;
            }

            // Deduplicate multi-file partial declarations: only emit once from the primary syntax location
            if (typeSymbol.Locations.Length == 0 ||
                !ReferenceEquals(typeSyntax.SyntaxTree, typeSymbol.Locations[0].SourceTree) ||
                typeSyntax.Identifier.Span != typeSymbol.Locations[0].SourceSpan)
            {
                return null;
            }

            if (!typeSymbol.IsSupportedPartialHierarchy(out _))
            {
                return null;
            }

            var filterInterfaces = typeSymbol.GetImplementedGenericInterfaces(Constants.IFilterableMetadataName)
                .Where(static i => !i.TypeArguments[0].IsErrorType())
                .ToArray();
            var sortInterfaces = typeSymbol.GetImplementedGenericInterfaces(Constants.ISortableMetadataName)
                .Where(static i => !i.TypeArguments[0].IsErrorType())
                .ToArray();

            if (filterInterfaces.Length == 0 && sortInterfaces.Length == 0)
            {
                return null;
            }

            // Merge unique entity types across IFilterable<T> and ISortable<T>
            var entityTypes = new List<ITypeSymbol>();
            foreach (var iface in filterInterfaces.Concat(sortInterfaces))
            {
                var entity = iface.TypeArguments[0];
                if (!entityTypes.Any(existing => SymbolEqualityComparer.Default.Equals(existing, entity)))
                {
                    entityTypes.Add(entity);
                }
            }

            // Class-level options & groups
            var autoMapSameName = true;
            var enableNamingConventions = true;
            var defaultStringMode = 0;         // Equals
            var defaultStringIgnorePolicy = 0; // NullOrWhiteSpace
            var trimStringsByDefault = false;
            var caseInsensitiveStringsByDefault = false;
            var defaultLogic = 0;              // CombineLogic.And
            var autoMapSortableProperties = true;
            var groupConfigs = new Dictionary<string, int>(StringComparer.Ordinal);
            var defaultSortAttrs = new List<AttributeData>();
            var classSortAttrs = new List<AttributeData>();

            foreach (var attr in typeSymbol.GetAttributes())
            {
                var attrName = attr.AttributeClass?.GetFullyQualifiedMetadataName();
                if (attrName == Constants.FilterOptionsAttributeMetadataName)
                {
                    autoMapSameName = attr.GetNamedValue("AutoMapSameNameProperties", true);
                    enableNamingConventions = attr.GetNamedValue("EnableNamingConventions", true);
                    defaultStringMode = attr.GetEnumIntValue("DefaultStringMode", 0);
                    defaultStringIgnorePolicy = attr.GetEnumIntValue("DefaultStringIgnorePolicy", 0);
                    trimStringsByDefault = attr.GetNamedValue("TrimStringsByDefault", false);
                    caseInsensitiveStringsByDefault = attr.GetNamedValue("CaseInsensitiveStringsByDefault", false);
                    defaultLogic = attr.GetEnumIntValue("DefaultLogic", 0);
                    autoMapSortableProperties = attr.GetNamedValue("AutoMapSortableProperties", true);
                }
                else if (attrName == Constants.FilterGroupAttributeMetadataName)
                {
                    var groupName = attr.GetStringValue("Name", fallbackCtorIndex: 0);
                    if (groupName is not null && !string.IsNullOrWhiteSpace(groupName))
                    {
                        groupConfigs[groupName] = attr.GetEnumIntValue("Logic", defaultValue: 1, ctorIndex: 1);
                    }
                }
                else if (attrName == Constants.DefaultSortAttributeMetadataName)
                {
                    defaultSortAttrs.Add(attr);
                }
                else if (attrName == Constants.SortFieldAttributeMetadataName)
                {
                    classSortAttrs.Add(attr);
                }
            }

            var compilation = context.SemanticModel.Compilation;
            var implementsSortableAny = sortInterfaces.Length > 0;
            var implementsPaged = typeSymbol.AllInterfaces.Any(static i => i.GetFullyQualifiedMetadataName() == Constants.IPagedFilterMetadataName);

            // Collect ordered instance properties from base types down to the derived type
            var orderedProperties = CollectPropertiesInDeclarationOrder(compilation, typeSymbol);
            var customMethods = CollectCustomFilterMethods(compilation, typeSymbol);

            var useExplicitFilterInterface = filterInterfaces.Length > 1;
            var targetSpecs = ImmutableArray.CreateBuilder<EntityTargetSpec>(entityTypes.Count);

            foreach (var entityType in entityTypes)
            {
                ct.ThrowIfCancellationRequested();

                var implementsFilter = filterInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], entityType));
                var implementsSort = sortInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], entityType));

                var hasUserToExpression = HasUserDefinedMethod(typeSymbol, "ToExpression", parameterType: null);
                var hasUserApply = HasUserDefinedQueryableMethod(typeSymbol, "Apply", entityType);
                var hasUserApplySort = HasUserDefinedQueryableMethod(typeSymbol, "ApplySort", entityType);

                var rules = ImmutableArray.CreateBuilder<FilterRuleSpec>();
                var localGroups = new Dictionary<string, int>(groupConfigs, StringComparer.Ordinal);

                if (implementsFilter)
                {
                    foreach (var prop in orderedProperties)
                    {
                        var rule = TryCreatePropertyRule(
                            compilation,
                            entityType,
                            prop,
                            implementsSortableAny,
                            implementsPaged,
                            autoMapSameName,
                            enableNamingConventions,
                            defaultStringMode,
                            defaultStringIgnorePolicy,
                            trimStringsByDefault,
                            caseInsensitiveStringsByDefault,
                            localGroups);

                        if (rule is not null)
                        {
                            rules.Add(rule);
                        }
                    }

                    foreach (var method in customMethods)
                    {
                        if (MatchesCustomFilterEntity(method, entityType))
                        {
                            var customRule = CreateCustomMethodRule(method, localGroups);
                            rules.Add(customRule);
                        }
                    }
                }

                var sortFields = ImmutableArray.CreateBuilder<SortFieldSpec>();
                var defaultSorts = ImmutableArray.CreateBuilder<DefaultSortSpec>();

                if (implementsSort)
                {
                    BuildSortMetadata(
                        compilation,
                        entityType,
                        orderedProperties,
                        classSortAttrs,
                        defaultSortAttrs,
                        autoMapSortableProperties,
                        sortFields,
                        defaultSorts);
                }

                var groupList = localGroups
                    .Select(static kvp => new FilterGroupSpec(kvp.Key, kvp.Value))
                    .ToImmutableArray();

                targetSpecs.Add(new EntityTargetSpec(
                    EntityFullName: entityType.GetFullyQualifiedName(),
                    EntitySimpleName: entityType.Name,
                    ImplementsFilterable: implementsFilter,
                    HasUserToExpression: hasUserToExpression && !useExplicitFilterInterface,
                    HasUserApply: hasUserApply,
                    UseExplicitFilterInterface: useExplicitFilterInterface,
                    ImplementsSortable: implementsSort,
                    HasUserApplySort: hasUserApplySort,
                    DefaultLogicValue: defaultLogic,
                    Rules: rules.ToImmutable(),
                    Groups: groupList,
                    SortFields: sortFields.ToImmutable(),
                    DefaultSorts: defaultSorts.ToImmutable()));
            }

            if (targetSpecs.Count == 0)
            {
                return null;
            }

            return new FilterGenerationSpec(
                HierarchyInfo.From(typeSymbol),
                targetSpecs.ToImmutable());
        }

        private static List<IPropertySymbol> CollectPropertiesInDeclarationOrder(Compilation compilation, INamedTypeSymbol typeSymbol)
        {
            var typeChain = new Stack<INamedTypeSymbol>();
            for (var current = typeSymbol; current is not null && current.SpecialType == SpecialType.None; current = current.BaseType)
            {
                typeChain.Push(current);
            }

            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            var list = new List<IPropertySymbol>();

            while (typeChain.Count > 0)
            {
                var current = typeChain.Pop();
                foreach (var member in current.GetMembers())
                {
                    if (member is IPropertySymbol
                        {
                            IsStatic: false,
                            IsIndexer: false,
                            GetMethod: not null,
                            DeclaredAccessibility: Accessibility.Public,
                            ExplicitInterfaceImplementations.Length: 0,
                        } prop &&
                        !prop.IsErrorType() &&
                        compilation.IsSymbolAccessibleWithin(prop, compilation.Assembly))
                    {
                        if (seen.TryGetValue(prop.Name, out var existingIdx))
                        {
                            list[existingIdx] = prop;
                        }
                        else
                        {
                            seen[prop.Name] = list.Count;
                            list.Add(prop);
                        }
                    }
                }
            }

            return list;
        }

        private static List<IMethodSymbol> CollectCustomFilterMethods(Compilation compilation, INamedTypeSymbol typeSymbol)
        {
            var methods = new List<IMethodSymbol>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var current = typeSymbol; current is not null && current.SpecialType == SpecialType.None; current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is IMethodSymbol
                        {
                            IsStatic: false,
                            IsGenericMethod: false,
                            Parameters.Length: 0,
                        } method &&
                        !method.IsErrorType() &&
                        compilation.IsSymbolAccessibleWithin(method, compilation.Assembly) &&
                        method.GetAttributes().Any(static a => a.AttributeClass?.GetFullyQualifiedMetadataName() == Constants.CustomFilterAttributeMetadataName) &&
                        seen.Add(method.Name))
                    {
                        methods.Add(method);
                    }
                }
            }

            return methods;
        }

        private static bool MatchesCustomFilterEntity(IMethodSymbol method, ITypeSymbol entityType)
        {
            return method.ReturnType is INamedTypeSymbol { TypeArguments.Length: 1 } exprType &&
                   exprType.GetFullyQualifiedMetadataName() == "System.Linq.Expressions.Expression`1" &&
                   exprType.TypeArguments[0] is INamedTypeSymbol { TypeArguments.Length: 2 } funcType &&
                   funcType.GetFullyQualifiedMetadataName() == "System.Func`2" &&
                   funcType.TypeArguments[1].SpecialType == SpecialType.System_Boolean &&
                   SymbolEqualityComparer.Default.Equals(funcType.TypeArguments[0], entityType);
        }

        private static FilterRuleSpec CreateCustomMethodRule(IMethodSymbol method, Dictionary<string, int> localGroups)
        {
            string? groupName = null;
            foreach (var attr in method.GetAttributes())
            {
                if (attr.AttributeClass?.GetFullyQualifiedMetadataName() == Constants.CustomFilterAttributeMetadataName)
                {
                    var grp = attr.GetStringValue("Group");
                    if (grp is not null && !string.IsNullOrWhiteSpace(grp))
                    {
                        groupName = grp;
                        if (!localGroups.ContainsKey(grp))
                        {
                            localGroups[grp] = 1; // Default Or
                        }
                    }

                    break;
                }
            }

            return new FilterRuleSpec(
                Kind: FilterRuleKind.CustomMethod,
                MemberName: method.Name,
                MemberTypeFullName: string.Empty,
                MemberUnwrappedTypeFullName: string.Empty,
                IsMemberNullableValueType: false,
                IsMemberReferenceType: true,
                CollectionStorageKind: 0,
                CollectionElementTypeFullName: null,
                IsCollectionElementNullableValueType: false,
                TargetPath: new PropertyPathSpec(ImmutableArray<PropertySegmentSpec>.Empty),
                MultiFieldPaths: ImmutableArray<PropertyPathSpec>.Empty,
                ElementSubPath: null,
                StringMode: 0,
                StringIgnorePolicy: 0,
                TrimString: false,
                CaseInsensitive: false,
                CompareOp: 0,
                IgnoreDefaultValue: false,
                MinInclusive: true,
                MaxInclusive: true,
                DatePrecisionValue: 0,
                DateKind: DateTypeKind.None,
                CombineLogicValue: 0,
                QuantifierValue: 0,
                NullBehaviorValue: 0,
                FlagsModeValue: 0,
                EnumUnderlyingTypeFullName: null,
                GroupName: groupName);
        }

        private static FilterRuleSpec? TryCreatePropertyRule(
            Compilation compilation,
            ITypeSymbol entityType,
            IPropertySymbol prop,
            bool implementsSortable,
            bool implementsPaged,
            bool autoMapSameName,
            bool enableNamingConventions,
            int defaultStringMode,
            int defaultStringIgnorePolicy,
            bool trimStringsByDefault,
            bool caseInsensitiveStringsByDefault,
            Dictionary<string, int> groupConfigs)
        {
            var attrs = prop.GetAttributes();
            var hasIgnore = false;
            var primaryAttrs = new List<(string Name, AttributeData Data)>();
            string? propGroupName = null;

            foreach (var attr in attrs)
            {
                var name = attr.AttributeClass?.GetFullyQualifiedMetadataName();
                if (name is null)
                {
                    continue;
                }

                if (name == Constants.FilterIgnoreAttributeMetadataName)
                {
                    hasIgnore = true;
                }
                else if (name == Constants.FilterGroupAttributeMetadataName)
                {
                    var grp = attr.GetStringValue("Name", fallbackCtorIndex: 0);
                    if (grp is not null && !string.IsNullOrWhiteSpace(grp))
                    {
                        propGroupName = grp;
                        if (!groupConfigs.ContainsKey(grp))
                        {
                            groupConfigs[grp] = attr.GetEnumIntValue("Logic", defaultValue: 1, ctorIndex: 1);
                        }
                    }
                }
                else if (Constants.PrimaryFilterAttributeNames.Contains(name))
                {
                    primaryAttrs.Add((name, attr));
                }
            }

            if (hasIgnore || primaryAttrs.Count > 1)
            {
                return null;
            }

            var unwrappedPropType = prop.Type.UnwrapNullableValueType(out var isNullableVal);
            var memberFullType = prop.Type.GetFullyQualifiedName();
            var memberUnwrappedFullType = unwrappedPropType.GetFullyQualifiedName();
            var isRefType = prop.Type.IsReferenceType;
            var (collKind, collElemFull, isCollElemNullVal) = GetCollectionDescriptor(prop.Type);

            if (primaryAttrs.Count == 1)
            {
                var (attrName, attrData) = primaryAttrs[0];
                var explicitGroup = attrData.GetStringValue("Group") ?? propGroupName;
                if (explicitGroup is not null && !string.IsNullOrWhiteSpace(explicitGroup) && !groupConfigs.ContainsKey(explicitGroup))
                {
                    groupConfigs[explicitGroup] = 1; // Default Or
                }

                return TryCreateAnnotatedRule(
                    compilation,
                    entityType,
                    prop,
                    attrName,
                    attrData,
                    memberFullType,
                    memberUnwrappedFullType,
                    isNullableVal,
                    isRefType,
                    collKind,
                    collElemFull,
                    isCollElemNullVal,
                    defaultStringMode,
                    defaultStringIgnorePolicy,
                    trimStringsByDefault,
                    caseInsensitiveStringsByDefault,
                    explicitGroup);
            }

            // Convention-based property
            if (PropertyPathResolver.IsFrameworkControlProperty(prop, implementsSortable, implementsPaged) ||
                attrs.Any(static a => a.AttributeClass?.GetFullyQualifiedMetadataName() == Constants.SortFieldAttributeMetadataName))
            {
                return null;
            }

            if (!PropertyPathResolver.TryResolveConventionTarget(
                    compilation,
                    entityType,
                    prop,
                    autoMapSameName,
                    enableNamingConventions,
                    out var resolvedSegments,
                    out var inferredOp))
            {
                return null;
            }

            var targetLeaf = resolvedSegments[^1].PropertyType;
            var pathSpec = ToPathSpec(resolvedSegments);

            if (propGroupName is not null && !string.IsNullOrWhiteSpace(propGroupName) && !groupConfigs.ContainsKey(propGroupName))
            {
                groupConfigs[propGroupName] = 1;
            }

            // 1. ValueRange<T>
            if (prop.Type.TryGetValueRangeElementType(out var rangeElem) && rangeElem is not null)
            {
                if (!rangeElem.IsScalarCompatibleWith(targetLeaf) || !targetLeaf.SupportsRelationalOperators())
                {
                    return null;
                }

                return CreateBaseRule(
                    FilterRuleKind.Range,
                    prop.Name,
                    memberFullType,
                    rangeElem.GetFullyQualifiedName(),
                    isNullableVal,
                    isRefType,
                    pathSpec,
                    propGroupName) with
                {
                    MinInclusive = true,
                    MaxInclusive = true,
                };
            }

            // 2. Collection -> In
            if (collKind != 0 &&
                prop.Type.TryGetCollectionElementType(out var elemSym) &&
                elemSym is not null &&
                !targetLeaf.TryGetCollectionElementType(out _))
            {
                if (!elemSym.IsScalarCompatibleWith(targetLeaf))
                {
                    return null;
                }

                return CreateBaseRule(
                    FilterRuleKind.In,
                    prop.Name,
                    memberFullType,
                    memberUnwrappedFullType,
                    isNullableVal,
                    isRefType,
                    pathSpec,
                    propGroupName) with
                {
                    CollectionStorageKind = collKind,
                    CollectionElementTypeFullName = collElemFull,
                    IsCollectionElementNullableValueType = isCollElemNullVal,
                };
            }

            // 3. String
            if (prop.Type.IsStringType())
            {
                if (!targetLeaf.IsStringType())
                {
                    return null;
                }

                return CreateBaseRule(
                    FilterRuleKind.String,
                    prop.Name,
                    memberFullType,
                    memberUnwrappedFullType,
                    isNullableVal,
                    isRefType,
                    pathSpec,
                    propGroupName) with
                {
                    StringMode = defaultStringMode,
                    StringIgnorePolicy = defaultStringIgnorePolicy,
                    TrimString = trimStringsByDefault,
                    CaseInsensitive = caseInsensitiveStringsByDefault,
                };
            }

            // 4. Scalar / Comparable
            if (!prop.Type.IsScalarCompatibleWith(targetLeaf))
            {
                return null;
            }

            var effectiveOp = inferredOp ?? 0;
            if (effectiveOp is 2 or 3 or 4 or 5 && !targetLeaf.SupportsRelationalOperators())
            {
                return null;
            }

            return CreateBaseRule(
                FilterRuleKind.Compare,
                prop.Name,
                memberFullType,
                memberUnwrappedFullType,
                isNullableVal,
                isRefType,
                pathSpec,
                propGroupName) with
            {
                CompareOp = effectiveOp,
            };
        }

        private static FilterRuleSpec? TryCreateAnnotatedRule(
            Compilation compilation,
            ITypeSymbol entityType,
            IPropertySymbol prop,
            string attrName,
            AttributeData attrData,
            string memberFullType,
            string memberUnwrappedFullType,
            bool isNullableVal,
            bool isRefType,
            int collKind,
            string? collElemFull,
            bool isCollElemNullVal,
            int defaultStringMode,
            int defaultStringIgnorePolicy,
            bool trimStringsByDefault,
            bool caseInsensitiveStringsByDefault,
            string? groupName)
        {
            if (attrName == Constants.MultiFieldFilterAttributeMetadataName)
            {
                return TryCreateMultiFieldRule(
                    compilation,
                    entityType,
                    prop,
                    attrData,
                    memberFullType,
                    memberUnwrappedFullType,
                    isNullableVal,
                    isRefType,
                    defaultStringIgnorePolicy,
                    trimStringsByDefault,
                    caseInsensitiveStringsByDefault,
                    groupName);
            }

            if (attrName == Constants.CollectionFilterAttributeMetadataName)
            {
                return TryCreateCollectionRule(
                    compilation,
                    entityType,
                    prop,
                    attrData,
                    memberFullType,
                    memberUnwrappedFullType,
                    isNullableVal,
                    isRefType,
                    collKind,
                    collElemFull,
                    isCollElemNullVal,
                    defaultStringIgnorePolicy,
                    trimStringsByDefault,
                    caseInsensitiveStringsByDefault,
                    groupName);
            }

            // Resolve single target path
            var explicitTarget = attrData.GetTargetPropertyPath();
            ImmutableArray<ResolvedPropertySegment> segments;
            if (!string.IsNullOrWhiteSpace(explicitTarget))
            {
                if (!PropertyPathResolver.TryResolvePath(compilation, entityType, explicitTarget!, out segments, out _))
                {
                    return null;
                }
            }
            else if (!PropertyPathResolver.TryResolveConventionTarget(
                         compilation,
                         entityType,
                         prop,
                         autoMapSameName: true,
                         enableNamingConventions: true,
                         out segments,
                         out _))
            {
                return null;
            }

            var targetLeaf = segments[^1].PropertyType;
            var pathSpec = ToPathSpec(segments);

            switch (attrName)
            {
                case Constants.StringFilterAttributeMetadataName:
                    if (!prop.Type.IsStringType() || !targetLeaf.IsStringType())
                    {
                        return null;
                    }

                    return CreateBaseRule(FilterRuleKind.String, prop.Name, memberFullType, memberUnwrappedFullType, isNullableVal, isRefType, pathSpec, groupName) with
                    {
                        StringMode = attrData.GetEnumIntValue("Mode", defaultValue: 2, ctorIndex: 0),
                        StringIgnorePolicy = attrData.GetEnumIntValue("IgnorePolicy", defaultValue: defaultStringIgnorePolicy),
                        TrimString = attrData.GetNamedValue("Trim", trimStringsByDefault),
                        CaseInsensitive = attrData.GetNamedValue("CaseInsensitive", caseInsensitiveStringsByDefault),
                    };

                case Constants.RangeFilterAttributeMetadataName:
                {
                    if (!prop.Type.TryGetValueRangeElementType(out var rangeElem) ||
                        rangeElem is null ||
                        !rangeElem.IsScalarCompatibleWith(targetLeaf) ||
                        !targetLeaf.SupportsRelationalOperators())
                    {
                        return null;
                    }

                    var rangePrecision = attrData.GetEnumIntValue("Precision", defaultValue: 0);
                    var rangeDateKind = DateTypeKind.None;
                    if (rangePrecision != 0)
                    {
                        if (!rangeElem.IsDateOrTimeType(out rangeDateKind) ||
                            !targetLeaf.IsDateOrTimeType(out var targetRangeDateKind) ||
                            rangeDateKind != targetRangeDateKind)
                        {
                            return null;
                        }
                    }

                    return CreateBaseRule(FilterRuleKind.Range, prop.Name, memberFullType, rangeElem.GetFullyQualifiedName(), isNullableVal, isRefType, pathSpec, groupName) with
                    {
                        MinInclusive = attrData.GetNamedValue("MinInclusive", true),
                        MaxInclusive = attrData.GetNamedValue("MaxInclusive", true),
                        DatePrecisionValue = rangePrecision,
                        DateKind = rangeDateKind,
                    };
                }

                case Constants.DateFilterAttributeMetadataName:
                    if (!prop.Type.IsDateOrTimeType(out var propDateKind) ||
                        !targetLeaf.IsDateOrTimeType(out var targetDateKind) ||
                        propDateKind != targetDateKind)
                    {
                        return null;
                    }

                    return CreateBaseRule(FilterRuleKind.Date, prop.Name, memberFullType, memberUnwrappedFullType, isNullableVal, isRefType, pathSpec, groupName) with
                    {
                        DatePrecisionValue = attrData.GetEnumIntValue("Precision", defaultValue: 3, ctorIndex: 0),
                        CompareOp = attrData.GetEnumIntValue("Operator", defaultValue: 0),
                        IgnoreDefaultValue = attrData.GetNamedValue("IgnoreDefaultValue", false),
                        DateKind = propDateKind,
                    };

                case Constants.NullCheckFilterAttributeMetadataName:
                    if (!prop.Type.IsBooleanType() || !segments[^1].IsReferenceOrNullableValue)
                    {
                        return null;
                    }

                    return CreateBaseRule(FilterRuleKind.NullCheck, prop.Name, memberFullType, memberUnwrappedFullType, isNullableVal, isRefType, pathSpec, groupName) with
                    {
                        NullBehaviorValue = attrData.GetEnumIntValue("Behavior", defaultValue: 0),
                    };

                case Constants.FlagsFilterAttributeMetadataName:
                    if (!prop.Type.IsEnumType(out var propEnum) ||
                        !targetLeaf.IsEnumType(out var targetEnum) ||
                        propEnum is null ||
                        !SymbolEqualityComparer.Default.Equals(propEnum, targetEnum))
                    {
                        return null;
                    }

                    return CreateBaseRule(FilterRuleKind.Flags, prop.Name, memberFullType, memberUnwrappedFullType, isNullableVal, isRefType, pathSpec, groupName) with
                    {
                        FlagsModeValue = attrData.GetEnumIntValue("Mode", defaultValue: 0, ctorIndex: 0),
                        IgnoreDefaultValue = attrData.GetNamedValue("IgnoreZero", true),
                        EnumUnderlyingTypeFullName = (propEnum.EnumUnderlyingType ?? compilation.GetSpecialType(SpecialType.System_Int32)).GetFullyQualifiedName(),
                    };

                case Constants.InFilterAttributeMetadataName:
                case Constants.NotInFilterAttributeMetadataName:
                    if (collKind == 0 ||
                        !prop.Type.TryGetCollectionElementType(out var collElem) ||
                        collElem is null ||
                        !collElem.IsScalarCompatibleWith(targetLeaf))
                    {
                        return null;
                    }

                    return CreateBaseRule(
                        attrName == Constants.InFilterAttributeMetadataName ? FilterRuleKind.In : FilterRuleKind.NotIn,
                        prop.Name,
                        memberFullType,
                        memberUnwrappedFullType,
                        isNullableVal,
                        isRefType,
                        pathSpec,
                        groupName) with
                    {
                        CollectionStorageKind = collKind,
                        CollectionElementTypeFullName = collElemFull,
                        IsCollectionElementNullableValueType = isCollElemNullVal,
                    };

                case Constants.CompareFilterAttributeMetadataName:
                {
                    var op = attrData.GetEnumIntValue("Operator", defaultValue: 0, ctorIndex: 0);
                    if (!prop.Type.IsScalarCompatibleWith(targetLeaf) ||
                        (op is 2 or 3 or 4 or 5 && (!prop.Type.SupportsRelationalOperators() || !targetLeaf.SupportsRelationalOperators())))
                    {
                        return null;
                    }

                    return CreateBaseRule(FilterRuleKind.Compare, prop.Name, memberFullType, memberUnwrappedFullType, isNullableVal, isRefType, pathSpec, groupName) with
                    {
                        CompareOp = op,
                        IgnoreDefaultValue = attrData.GetNamedValue("IgnoreDefaultValue", false),
                    };
                }

                case Constants.FilterFieldAttributeMetadataName:
                {
                    var ignoreDefault = attrData.GetNamedValue("IgnoreDefaultValue", false);
                    if (prop.Type.TryGetValueRangeElementType(out var ffRange) && ffRange is not null)
                    {
                        if (!ffRange.IsScalarCompatibleWith(targetLeaf) || !targetLeaf.SupportsRelationalOperators())
                        {
                            return null;
                        }

                        return CreateBaseRule(FilterRuleKind.Range, prop.Name, memberFullType, ffRange.GetFullyQualifiedName(), isNullableVal, isRefType, pathSpec, groupName);
                    }

                    if (collKind != 0 &&
                        prop.Type.TryGetCollectionElementType(out var ffColl) &&
                        ffColl is not null &&
                        !targetLeaf.TryGetCollectionElementType(out _))
                    {
                        if (!ffColl.IsScalarCompatibleWith(targetLeaf))
                        {
                            return null;
                        }

                        return CreateBaseRule(FilterRuleKind.In, prop.Name, memberFullType, memberUnwrappedFullType, isNullableVal, isRefType, pathSpec, groupName) with
                        {
                            CollectionStorageKind = collKind,
                            CollectionElementTypeFullName = collElemFull,
                            IsCollectionElementNullableValueType = isCollElemNullVal,
                        };
                    }

                    if (prop.Type.IsStringType())
                    {
                        if (!targetLeaf.IsStringType())
                        {
                            return null;
                        }

                        return CreateBaseRule(FilterRuleKind.String, prop.Name, memberFullType, memberUnwrappedFullType, isNullableVal, isRefType, pathSpec, groupName) with
                        {
                            StringMode = defaultStringMode,
                            StringIgnorePolicy = defaultStringIgnorePolicy,
                            TrimString = trimStringsByDefault,
                            CaseInsensitive = caseInsensitiveStringsByDefault,
                        };
                    }

                    if (!prop.Type.IsScalarCompatibleWith(targetLeaf))
                    {
                        return null;
                    }

                    return CreateBaseRule(FilterRuleKind.Compare, prop.Name, memberFullType, memberUnwrappedFullType, isNullableVal, isRefType, pathSpec, groupName) with
                    {
                        IgnoreDefaultValue = ignoreDefault,
                    };
                }

                default:
                    return null;
            }
        }

        private static FilterRuleSpec? TryCreateMultiFieldRule(
            Compilation compilation,
            ITypeSymbol entityType,
            IPropertySymbol prop,
            AttributeData attrData,
            string memberFullType,
            string memberUnwrappedFullType,
            bool isNullableVal,
            bool isRefType,
            int defaultStringIgnorePolicy,
            bool trimStringsByDefault,
            bool caseInsensitiveStringsByDefault,
            string? groupName)
        {
            if (attrData.ConstructorArguments.Length == 0 ||
                attrData.ConstructorArguments[0].Kind != TypedConstantKind.Array)
            {
                return null;
            }

            var rawValues = attrData.ConstructorArguments[0].Values;
            var paths = ImmutableArray.CreateBuilder<PropertyPathSpec>(rawValues.Length);
            var mode = attrData.GetEnumIntValue("Mode", defaultValue: 2); // Contains
            var op = attrData.GetEnumIntValue("Operator", defaultValue: 0);

            foreach (var item in rawValues)
            {
                if (item.Value is not string fieldPath || string.IsNullOrWhiteSpace(fieldPath))
                {
                    continue;
                }

                if (!PropertyPathResolver.TryResolvePath(compilation, entityType, fieldPath, out var segments, out _))
                {
                    return null;
                }

                var targetType = segments[^1].PropertyType;
                if (prop.Type.IsStringType())
                {
                    if (!targetType.IsStringType())
                    {
                        return null;
                    }
                }
                else if (!prop.Type.IsScalarCompatibleWith(targetType) ||
                         (op is 2 or 3 or 4 or 5 && (!prop.Type.SupportsRelationalOperators() || !targetType.SupportsRelationalOperators())))
                {
                    return null;
                }

                paths.Add(ToPathSpec(segments));
            }

            if (paths.Count == 0)
            {
                return null;
            }

            return CreateBaseRule(
                FilterRuleKind.MultiField,
                prop.Name,
                memberFullType,
                memberUnwrappedFullType,
                isNullableVal,
                isRefType,
                paths[0],
                groupName) with
            {
                MultiFieldPaths = paths.ToImmutable(),
                StringMode = mode,
                StringIgnorePolicy = attrData.GetEnumIntValue("IgnorePolicy", defaultValue: defaultStringIgnorePolicy),
                TrimString = attrData.GetNamedValue("Trim", trimStringsByDefault),
                CaseInsensitive = attrData.GetNamedValue("CaseInsensitive", caseInsensitiveStringsByDefault),
                CompareOp = op,
                IgnoreDefaultValue = attrData.GetNamedValue("IgnoreDefaultValue", false),
                CombineLogicValue = attrData.GetEnumIntValue("Logic", defaultValue: 1),
            };
        }

        private static FilterRuleSpec? TryCreateCollectionRule(
            Compilation compilation,
            ITypeSymbol entityType,
            IPropertySymbol prop,
            AttributeData attrData,
            string memberFullType,
            string memberUnwrappedFullType,
            bool isNullableVal,
            bool isRefType,
            int collKind,
            string? collElemFull,
            bool isCollElemNullVal,
            int defaultStringIgnorePolicy,
            bool trimStringsByDefault,
            bool caseInsensitiveStringsByDefault,
            string? groupName)
        {
            attrData.GetCollectionFilterArgs(out var navPath, out var quantifier, out var elementPath);
            if (navPath is null || string.IsNullOrWhiteSpace(navPath) ||
                !PropertyPathResolver.TryResolvePath(compilation, entityType, navPath, out var navSegments, out _))
            {
                return null;
            }

            if (!navSegments[^1].PropertyType.TryGetCollectionElementType(out var elementType) || elementType is null)
            {
                return null;
            }

            PropertyPathSpec? subPathSpec = null;
            ITypeSymbol targetScalarType;

            if (elementPath is not null && !string.IsNullOrWhiteSpace(elementPath))
            {
                if (!PropertyPathResolver.TryResolvePath(compilation, elementType, elementPath, out var elemSegments, out _))
                {
                    return null;
                }

                subPathSpec = ToPathSpec(elemSegments);
                targetScalarType = elemSegments[^1].PropertyType;
            }
            else
            {
                targetScalarType = elementType;
            }

            var op = attrData.GetEnumIntValue("Operator", defaultValue: 0);
            if (collKind != 0 && prop.Type.TryGetCollectionElementType(out var dtoElem) && dtoElem is not null)
            {
                if (!dtoElem.IsScalarCompatibleWith(targetScalarType))
                {
                    return null;
                }
            }
            else if (prop.Type.IsStringType())
            {
                if (!targetScalarType.IsStringType())
                {
                    return null;
                }
            }
            else if (!prop.Type.IsScalarCompatibleWith(targetScalarType) ||
                     (op is 2 or 3 or 4 or 5 && !targetScalarType.SupportsRelationalOperators()))
            {
                return null;
            }

            _ = targetScalarType.UnwrapNullableValueType(out var isTargetScalarNullVal);

            return CreateBaseRule(
                FilterRuleKind.Collection,
                prop.Name,
                memberFullType,
                memberUnwrappedFullType,
                isNullableVal,
                isRefType,
                ToPathSpec(navSegments),
                groupName) with
            {
                ElementSubPath = subPathSpec,
                CollectionStorageKind = collKind,
                CollectionElementTypeFullName = collElemFull ?? targetScalarType.GetFullyQualifiedName(),
                IsCollectionElementNullableValueType = isCollElemNullVal || isTargetScalarNullVal,
                QuantifierValue = quantifier,
                StringMode = attrData.GetEnumIntValue("StringMode", defaultValue: 0),
                StringIgnorePolicy = attrData.GetEnumIntValue("IgnorePolicy", defaultValue: defaultStringIgnorePolicy),
                TrimString = attrData.GetNamedValue("Trim", trimStringsByDefault),
                CaseInsensitive = attrData.GetNamedValue("CaseInsensitive", caseInsensitiveStringsByDefault),
                CompareOp = op,
                IgnoreDefaultValue = attrData.GetNamedValue("IgnoreDefaultValue", false),
            };
        }

        private static void BuildSortMetadata(
            Compilation compilation,
            ITypeSymbol entityType,
            List<IPropertySymbol> dtoProperties,
            List<AttributeData> classSortAttrs,
            List<AttributeData> defaultSortAttrs,
            bool autoMapSortableProperties,
            ImmutableArray<SortFieldSpec>.Builder sortFields,
            ImmutableArray<DefaultSortSpec>.Builder defaultSorts)
        {
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Explicit class-level [SortField]
            foreach (var attr in classSortAttrs)
            {
                var key = attr.GetStringValue("Name", fallbackCtorIndex: 0);
                var target = attr.GetStringValue("Target", fallbackCtorIndex: 1) ?? key;
                if (key is not null && !string.IsNullOrWhiteSpace(key) &&
                    target is not null && !string.IsNullOrWhiteSpace(target) &&
                    seenKeys.Add(key) &&
                    PropertyPathResolver.TryResolvePath(compilation, entityType, target, out var segs, out _))
                {
                    sortFields.Add(new SortFieldSpec(key.Trim().ToLowerInvariant(), ToPathSpec(segs)));
                }
            }

            // 2. Explicit property-level [SortField]
            foreach (var prop in dtoProperties)
            {
                if (prop.GetAttributes().Any(static a => a.AttributeClass?.GetFullyQualifiedMetadataName() == Constants.FilterIgnoreAttributeMetadataName))
                {
                    continue;
                }

                foreach (var attr in prop.GetAttributes())
                {
                    if (attr.AttributeClass?.GetFullyQualifiedMetadataName() == Constants.SortFieldAttributeMetadataName)
                    {
                        var key = attr.GetStringValue("Name", fallbackCtorIndex: 0) ?? prop.Name;
                        var target = attr.GetStringValue("Target", fallbackCtorIndex: 1) ?? key;
                        if (!string.IsNullOrWhiteSpace(key) &&
                            !string.IsNullOrWhiteSpace(target) &&
                            seenKeys.Add(key) &&
                            PropertyPathResolver.TryResolvePath(compilation, entityType, target, out var segs, out _))
                        {
                            sortFields.Add(new SortFieldSpec(key.Trim().ToLowerInvariant(), ToPathSpec(segs)));
                        }
                    }
                }
            }

            // 3. Accessible scalar/comparable properties directly on entityType (and its base types)
            if (autoMapSortableProperties)
            {
                for (var current = entityType as INamedTypeSymbol;
                     current is not null && current.SpecialType == SpecialType.None;
                     current = current.BaseType)
                {
                    foreach (var member in current.GetMembers())
                    {
                        if (member is IPropertySymbol { IsStatic: false, IsIndexer: false, GetMethod: not null } entityProp &&
                            compilation.IsSymbolAccessibleWithin(entityProp, compilation.Assembly) &&
                            IsSortablePropertyType(entityProp.Type) &&
                            seenKeys.Add(entityProp.Name))
                        {
                            var segs = ImmutableArray.Create(new ResolvedPropertySegment(
                                entityProp,
                                entityProp.Name,
                                entityProp.Type,
                                entityProp.Type.CanBeNull()));
                            sortFields.Add(new SortFieldSpec(entityProp.Name.ToLowerInvariant(), ToPathSpec(segs)));
                        }
                    }
                }
            }

            // 4. [DefaultSort] (supports multiple levels: OrderBy + ThenBy...)
            foreach (var defaultSortAttr in defaultSortAttrs)
            {
                var defTarget = defaultSortAttr.GetStringValue("Target", fallbackCtorIndex: 0);
                var defDesc = defaultSortAttr.GetNamedValue("Descending", defaultSortAttr.ConstructorArguments.Length > 1 && defaultSortAttr.ConstructorArguments[1].Value is true);
                if (defTarget is not null && !string.IsNullOrWhiteSpace(defTarget) &&
                    PropertyPathResolver.TryResolvePath(compilation, entityType, defTarget, out var defSegs, out _))
                {
                    defaultSorts.Add(new DefaultSortSpec(ToPathSpec(defSegs), defDesc));
                }
            }
        }

        private static bool IsSortablePropertyType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.IsStringType())
            {
                return true;
            }

            if (typeSymbol.TryGetCollectionElementType(out _))
            {
                return false;
            }

            var unwrapped = typeSymbol.UnwrapNullableValueType(out _);
            return unwrapped.IsValueType || unwrapped.SupportsRelationalOperators();
        }

        private static (int Kind, string? ElemFullType, bool IsElemNullableVal) GetCollectionDescriptor(ITypeSymbol type)
        {
            if (!type.TryGetCollectionElementType(out var elem) || elem is null)
            {
                return (0, null, false);
            }

            elem.UnwrapNullableValueType(out var isElemNullVal);
            var elemFull = elem.GetFullyQualifiedName();

            if (type is IArrayTypeSymbol)
            {
                return (1, elemFull, isElemNullVal);
            }

            if (type is INamedTypeSymbol named &&
                named.GetMembers("Count").OfType<IPropertySymbol>().Any(static p => p.Type.SpecialType == SpecialType.System_Int32 && p.DeclaredAccessibility == Accessibility.Public))
            {
                return (2, elemFull, isElemNullVal);
            }

            return (3, elemFull, isElemNullVal);
        }

        private static PropertyPathSpec ToPathSpec(ImmutableArray<ResolvedPropertySegment> segments)
        {
            var builder = ImmutableArray.CreateBuilder<PropertySegmentSpec>(segments.Length);
            foreach (var seg in segments)
            {
                var unwrapped = seg.PropertyType.UnwrapNullableValueType(out var isNullVal);
                builder.Add(new PropertySegmentSpec(
                    PropertyName: seg.Name,
                    FullyQualifiedTypeName: seg.PropertyType.GetFullyQualifiedName(),
                    UnwrappedTypeName: unwrapped.GetFullyQualifiedName(),
                    IsReferenceType: seg.PropertyType.IsReferenceType,
                    IsNullableValueType: isNullVal));
            }

            return new PropertyPathSpec(builder.ToImmutable());
        }

        private static FilterRuleSpec CreateBaseRule(
            FilterRuleKind kind,
            string memberName,
            string memberFullType,
            string memberUnwrappedFullType,
            bool isNullableVal,
            bool isRefType,
            PropertyPathSpec targetPath,
            string? groupName)
        {
            return new FilterRuleSpec(
                Kind: kind,
                MemberName: memberName,
                MemberTypeFullName: memberFullType,
                MemberUnwrappedTypeFullName: memberUnwrappedFullType,
                IsMemberNullableValueType: isNullableVal,
                IsMemberReferenceType: isRefType,
                CollectionStorageKind: 0,
                CollectionElementTypeFullName: null,
                IsCollectionElementNullableValueType: false,
                TargetPath: targetPath,
                MultiFieldPaths: ImmutableArray<PropertyPathSpec>.Empty,
                ElementSubPath: null,
                StringMode: 0,
                StringIgnorePolicy: 0,
                TrimString: false,
                CaseInsensitive: false,
                CompareOp: 0,
                IgnoreDefaultValue: false,
                MinInclusive: true,
                MaxInclusive: true,
                DatePrecisionValue: 0,
                DateKind: DateTypeKind.None,
                CombineLogicValue: 0,
                QuantifierValue: 0,
                NullBehaviorValue: 0,
                FlagsModeValue: 0,
                EnumUnderlyingTypeFullName: null,
                GroupName: string.IsNullOrWhiteSpace(groupName) ? null : groupName);
        }

        private static bool HasUserDefinedMethod(INamedTypeSymbol typeSymbol, string methodName, ITypeSymbol? parameterType)
        {
            foreach (var member in typeSymbol.GetMembers(methodName))
            {
                if (member is IMethodSymbol { IsStatic: false } method &&
                    !method.IsImplicitlyDeclared &&
                    (parameterType is null
                        ? method.Parameters.Length == 0
                        : method.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, parameterType)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUserDefinedQueryableMethod(INamedTypeSymbol typeSymbol, string methodName, ITypeSymbol entityType)
        {
            foreach (var member in typeSymbol.GetMembers(methodName))
            {
                if (member is IMethodSymbol { IsStatic: false, Parameters.Length: 1 } method &&
                    !method.IsImplicitlyDeclared &&
                    method.Parameters[0].Type is INamedTypeSymbol { TypeArguments.Length: 1 } paramType &&
                    paramType.GetFullyQualifiedMetadataName() == "System.Linq.IQueryable`1" &&
                    SymbolEqualityComparer.Default.Equals(paramType.TypeArguments[0], entityType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

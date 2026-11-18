using Ling.Filter.Generators.Infrastructure;
using Ling.Filter.Helpers;

namespace Ling.Filter.Generators.Models;

/// <summary>
/// Immutable, value-equatable specification for a single filter property or custom filter method.
/// </summary>
internal sealed record FilterRuleSpec(
    FilterRuleKind Kind,
    string MemberName,
    string MemberTypeFullName,
    string MemberUnwrappedTypeFullName,
    bool IsMemberNullableValueType,
    bool IsMemberReferenceType,
    int CollectionStorageKind, // 0 = None, 1 = Array, 2 = ListOrReadOnlyCollectionWithCount, 3 = GeneralEnumerable
    string? CollectionElementTypeFullName,
    bool IsCollectionElementNullableValueType,
    PropertyPathSpec TargetPath,
    EquatableArray<PropertyPathSpec> MultiFieldPaths,
    PropertyPathSpec? ElementSubPath,
    int StringMode,         // StringFilterMode
    int StringIgnorePolicy, // StringIgnorePolicy
    bool TrimString,
    bool CaseInsensitive,
    int CompareOp,          // CompareOperator
    bool IgnoreDefaultValue,
    bool MinInclusive,
    bool MaxInclusive,
    int DatePrecisionValue, // DatePrecision
    DateTypeKind DateKind,
    int CombineLogicValue,  // CombineLogic
    int QuantifierValue,    // CollectionQuantifier
    int NullBehaviorValue,  // NullCheckBehavior
    int FlagsModeValue,     // FlagsMatchMode
    string? EnumUnderlyingTypeFullName,
    string? GroupName);

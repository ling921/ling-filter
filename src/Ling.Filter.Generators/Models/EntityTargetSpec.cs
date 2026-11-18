using Ling.Filter.Generators.Infrastructure;

namespace Ling.Filter.Generators.Models;

/// <summary>
/// Value-equatable generation specification for a single target entity on a filter/sortable model.
/// </summary>
internal sealed record EntityTargetSpec(
    string EntityFullName,
    string EntitySimpleName,
    bool ImplementsFilterable,
    bool HasUserToExpression,
    bool HasUserApply,
    bool UseExplicitFilterInterface,
    bool ImplementsSortable,
    bool HasUserApplySort,
    EquatableArray<FilterRuleSpec> Rules,
    EquatableArray<FilterGroupSpec> Groups,
    EquatableArray<SortFieldSpec> SortFields,
    PropertyPathSpec? DefaultSortPath,
    bool DefaultSortDescending);

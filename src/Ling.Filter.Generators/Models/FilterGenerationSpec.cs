using Ling.Filter.Generators.Infrastructure;

namespace Ling.Filter.Generators.Models;

/// <summary>
/// Top-level value-equatable source generation specification for a partial filter/sortable model.
/// </summary>
internal sealed record FilterGenerationSpec(
    HierarchyInfo Hierarchy,
    EquatableArray<EntityTargetSpec> Targets);

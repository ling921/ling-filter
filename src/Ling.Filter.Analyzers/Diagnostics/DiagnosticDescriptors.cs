using Microsoft.CodeAnalysis;

namespace Ling.Filter.Analyzers.Diagnostics;

/// <summary>
/// Provides all <see cref="DiagnosticDescriptor"/> definitions reported by <c>Ling.Filter.Analyzers</c>.
/// </summary>
public static class DiagnosticDescriptors
{
    /// <summary>
    /// Diagnostic <c>LF0001</c>: Filter or sortable type must be declared <c>partial</c> and non-generic.
    /// </summary>
    public static readonly DiagnosticDescriptor FilterTypeMustBePartial = new(
        id: DiagnosticIds.FilterTypeMustBePartialId,
        title: "Filter or sortable type must be partial and non-generic",
        messageFormat: "Filter model cannot be generated because {0}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types implementing IFilterable<TEntity> or ISortable<TEntity> (and any containing types) must be declared partial and non-generic.",
        helpLinkUri: Constants.RepositoryUrl);

    /// <summary>
    /// Diagnostic <c>LF0002</c>: Specified target property or navigation path does not exist on the target entity.
    /// </summary>
    public static readonly DiagnosticDescriptor TargetPropertyPathNotFound = new(
        id: DiagnosticIds.TargetPropertyPathNotFoundId,
        title: "Target property or navigation path not found on entity",
        messageFormat: "Property path '{0}' could not be resolved on entity type '{1}' (missing or inaccessible member '{2}')",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All property names and dot-separated navigation paths referenced by filter or sort attributes must exist and be readable on the target entity.",
        helpLinkUri: Constants.RepositoryUrl);

    /// <summary>
    /// Diagnostic <c>LF0003</c>: Filter attribute is incompatible with the annotated DTO property type.
    /// </summary>
    public static readonly DiagnosticDescriptor AttributeIncompatibleWithPropertyType = new(
        id: DiagnosticIds.AttributeIncompatibleWithPropertyTypeId,
        title: "Filter attribute is incompatible with DTO property type",
        messageFormat: "Attribute '{0}' cannot be applied to property '{1}' of type '{2}': {3}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Filter attributes must be applied to DTO properties of a compatible CLR type.",
        helpLinkUri: Constants.RepositoryUrl);

    /// <summary>
    /// Diagnostic <c>LF0004</c>: Filter property type cannot be compared with the target entity property type.
    /// </summary>
    public static readonly DiagnosticDescriptor FilterAndTargetTypesIncompatible = new(
        id: DiagnosticIds.FilterAndTargetTypesIncompatibleId,
        title: "Filter property type is incompatible with target entity property type",
        messageFormat: "Filter property '{0}' of type '{1}' cannot filter target '{2}' of type '{3}': {4}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The filter property type and the mapped entity property type must support the requested LINQ comparison.",
        helpLinkUri: Constants.RepositoryUrl);

    /// <summary>
    /// Diagnostic <c>LF0005</c>: Public readable property on a filter model is unmapped and lacks <c>[FilterIgnore]</c>.
    /// </summary>
    public static readonly DiagnosticDescriptor UnmappedFilterProperty = new(
        id: DiagnosticIds.UnmappedFilterPropertyId,
        title: "Public filter property is not mapped to any entity property",
        messageFormat: "Property '{0}' does not match any property on '{1}'; mark it with [FilterIgnore] or specify a [FilterField] target",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Unmapped public properties on filter models may indicate a typo or renamed entity member. Mark intentional non-filter properties with [FilterIgnore].",
        helpLinkUri: Constants.RepositoryUrl);

    /// <summary>
    /// Diagnostic <c>LF0006</c>: Method decorated with <c>[CustomFilter]</c> has an invalid signature.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidCustomFilterMethod = new(
        id: DiagnosticIds.InvalidCustomFilterMethodId,
        title: "CustomFilter method has an invalid signature",
        messageFormat: "Method '{0}' marked with [CustomFilter] {1}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Methods marked with [CustomFilter] must be accessible, non-static, non-generic, parameterless, and return Expression<Func<TEntity, bool>>? for an implemented IFilterable<TEntity>.",
        helpLinkUri: Constants.RepositoryUrl);

    /// <summary>
    /// Diagnostic <c>LF0007</c>: <c>[CollectionFilter]</c> target navigation is not a collection or element path is invalid.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidCollectionFilterTarget = new(
        id: DiagnosticIds.InvalidCollectionFilterTargetId,
        title: "Invalid CollectionFilter navigation or element path",
        messageFormat: "CollectionFilter on '{0}' is invalid: {1}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "CollectionFilter must reference a collection navigation property on the entity and a valid element property path.",
        helpLinkUri: Constants.RepositoryUrl);

    /// <summary>
    /// Diagnostic <c>LF0008</c>: Conflicting or mutually exclusive filter attributes are applied to the same property.
    /// </summary>
    public static readonly DiagnosticDescriptor ConflictingFilterAttributes = new(
        id: DiagnosticIds.ConflictingFilterAttributesId,
        title: "Conflicting filter attributes applied to property",
        messageFormat: "Property '{0}' cannot combine conflicting filter attributes ({1})",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "At most one primary filter attribute may be applied to a property, and [FilterIgnore] cannot be combined with filter attributes.",
        helpLinkUri: Constants.RepositoryUrl);

    /// <summary>
    /// Diagnostic <c>LF0009</c>: <c>[MultiFieldFilter]</c> specifies zero target fields.
    /// </summary>
    public static readonly DiagnosticDescriptor EmptyMultiFieldFilter = new(
        id: DiagnosticIds.EmptyMultiFieldFilterId,
        title: "MultiFieldFilter specifies no target fields",
        messageFormat: "Attribute 'MultiFieldFilter' on property '{0}' must specify at least one target field",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "MultiFieldFilter requires at least one non-empty entity field path.",
        helpLinkUri: Constants.RepositoryUrl);
}

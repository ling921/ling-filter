namespace Ling.Filter;

/// <summary>
/// Diagnostic identifier constants reported by <c>Ling.Filter.Analyzers</c>.
/// </summary>
internal static class DiagnosticIds
{
    /// <summary>
    /// Filter or sortable type (and any containing types) must be declared <c>partial</c> and non-generic.
    /// </summary>
    public const string FilterTypeMustBePartialId = "LF0001";

    /// <summary>
    /// Specified target property or navigation path does not exist or is inaccessible on the target entity.
    /// </summary>
    public const string TargetPropertyPathNotFoundId = "LF0002";

    /// <summary>
    /// Filter attribute is applied to an incompatible DTO property type.
    /// </summary>
    public const string AttributeIncompatibleWithPropertyTypeId = "LF0003";

    /// <summary>
    /// Filter property type cannot be compared against the mapped target entity property type.
    /// </summary>
    public const string FilterAndTargetTypesIncompatibleId = "LF0004";

    /// <summary>
    /// Public readable property on filter model could not be mapped to any entity property and lacks <c>[FilterIgnore]</c>.
    /// </summary>
    public const string UnmappedFilterPropertyId = "LF0005";

    /// <summary>
    /// Method marked with <c>[CustomFilter]</c> has an invalid signature.
    /// </summary>
    public const string InvalidCustomFilterMethodId = "LF0006";

    /// <summary>
    /// <c>[CollectionFilter]</c> navigation property is not a collection or element property path is invalid.
    /// </summary>
    public const string InvalidCollectionFilterTargetId = "LF0007";

    /// <summary>
    /// Conflicting or mutually exclusive filter attributes are applied to the same property.
    /// </summary>
    public const string ConflictingFilterAttributesId = "LF0008";

    /// <summary>
    /// <c>[MultiFieldFilter]</c> specifies zero target fields.
    /// </summary>
    public const string EmptyMultiFieldFilterId = "LF0009";
}

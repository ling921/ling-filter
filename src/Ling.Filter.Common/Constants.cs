using System.Collections.Immutable;

namespace Ling.Filter;

/// <summary>
/// Metadata name constants shared across <c>Ling.Filter</c> analyzers, code fixes, and source generators.
/// </summary>
internal static class Constants
{
    public const string RepositoryUrl = "https://github.com/ling921/ling-filter";

    public const string RootNamespace = "Ling.Filter";
    public const string AnnotationsNamespace = "Ling.Filter.Annotations";

    public const string IFilterableMetadataName = RootNamespace + ".IFilterable`1";
    public const string ISortableMetadataName = RootNamespace + ".ISortable`1";
    public const string IPagedFilterMetadataName = RootNamespace + ".IPagedFilter";
    public const string ValueRangeMetadataName = RootNamespace + ".ValueRange`1";

    public const string FilterBaseAttributeMetadataName = AnnotationsNamespace + ".FilterBaseAttribute";
    public const string FilterOptionsAttributeMetadataName = AnnotationsNamespace + ".FilterOptionsAttribute";
    public const string FilterIgnoreAttributeMetadataName = AnnotationsNamespace + ".FilterIgnoreAttribute";
    public const string FilterFieldAttributeMetadataName = AnnotationsNamespace + ".FilterFieldAttribute";
    public const string StringFilterAttributeMetadataName = AnnotationsNamespace + ".StringFilterAttribute";
    public const string CompareFilterAttributeMetadataName = AnnotationsNamespace + ".CompareFilterAttribute";
    public const string RangeFilterAttributeMetadataName = AnnotationsNamespace + ".RangeFilterAttribute";
    public const string DateFilterAttributeMetadataName = AnnotationsNamespace + ".DateFilterAttribute";
    public const string MultiFieldFilterAttributeMetadataName = AnnotationsNamespace + ".MultiFieldFilterAttribute";
    public const string FilterGroupAttributeMetadataName = AnnotationsNamespace + ".FilterGroupAttribute";
    public const string InFilterAttributeMetadataName = AnnotationsNamespace + ".InFilterAttribute";
    public const string NotInFilterAttributeMetadataName = AnnotationsNamespace + ".NotInFilterAttribute";
    public const string CollectionFilterAttributeMetadataName = AnnotationsNamespace + ".CollectionFilterAttribute";
    public const string NullCheckFilterAttributeMetadataName = AnnotationsNamespace + ".NullCheckFilterAttribute";
    public const string FlagsFilterAttributeMetadataName = AnnotationsNamespace + ".FlagsFilterAttribute";
    public const string CustomFilterAttributeMetadataName = AnnotationsNamespace + ".CustomFilterAttribute";
    public const string SortFieldAttributeMetadataName = AnnotationsNamespace + ".SortFieldAttribute";
    public const string DefaultSortAttributeMetadataName = AnnotationsNamespace + ".DefaultSortAttribute";

    /// <summary>
    /// Mutually exclusive primary filter attributes on a single property.
    /// </summary>
    public static readonly ImmutableHashSet<string> PrimaryFilterAttributeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        FilterFieldAttributeMetadataName,
        StringFilterAttributeMetadataName,
        CompareFilterAttributeMetadataName,
        RangeFilterAttributeMetadataName,
        DateFilterAttributeMetadataName,
        MultiFieldFilterAttributeMetadataName,
        InFilterAttributeMetadataName,
        NotInFilterAttributeMetadataName,
        CollectionFilterAttributeMetadataName,
        NullCheckFilterAttributeMetadataName,
        FlagsFilterAttributeMetadataName);
}

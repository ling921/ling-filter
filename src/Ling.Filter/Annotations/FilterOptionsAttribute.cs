namespace Ling.Filter.Annotations;

/// <summary>
/// Configures class-level defaults and conventions for a filter model implementing <see cref="IFilterable{TEntity}"/> or <see cref="ISortable{TEntity}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class FilterOptionsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether unannotated properties on the filter model automatically map
    /// to entity properties of the same name. Defaults to <see langword="true"/>.
    /// </summary>
    public bool AutoMapSameNameProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether naming conventions (<c>MinXxx</c>/<c>StartXxx</c>/<c>XxxFrom</c> as <c>&gt;=</c>
    /// and <c>MaxXxx</c>/<c>EndXxx</c>/<c>XxxTo</c> as <c>&lt;=</c>) are automatically recognized when no exact same-name property exists.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableNamingConventions { get; set; } = true;

    /// <summary>
    /// Gets or sets the default <see cref="StringFilterMode"/> used for auto-mapped string properties without <see cref="StringFilterAttribute"/>.
    /// Defaults to <see cref="StringFilterMode.Equals"/>.
    /// </summary>
    public StringFilterMode DefaultStringMode { get; set; } = StringFilterMode.Equals;

    /// <summary>
    /// Gets or sets the default <see cref="StringIgnorePolicy"/> used for string filter properties.
    /// Defaults to <see cref="StringIgnorePolicy.NullOrWhiteSpace"/>.
    /// </summary>
    public StringIgnorePolicy DefaultStringIgnorePolicy { get; set; } = StringIgnorePolicy.NullOrWhiteSpace;

    /// <summary>
    /// Gets or sets a value indicating whether string filter inputs are trimmed by default before comparison.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool TrimStringsByDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether string comparisons normalize both sides using <c>ToLower()</c> by default.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool CaseInsensitiveStringsByDefault { get; set; }

    /// <summary>
    /// Gets or sets the default logical operator used to combine top-level filter predicates and groups.
    /// Defaults to <see cref="CombineLogic.And"/>.
    /// </summary>
    public CombineLogic DefaultLogic { get; set; } = CombineLogic.And;

    /// <summary>
    /// Gets or sets a value indicating whether accessible scalar properties on the target entity are automatically
    /// exposed as allowed dynamic sort fields when implementing <see cref="ISortable{TEntity}"/>.
    /// Set to <see langword="false"/> to enforce a strict explicit <see cref="SortFieldAttribute"/> whitelist.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool AutoMapSortableProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the analyzer reports warning <c>LF0005</c> when a public readable property
    /// on the filter model cannot be mapped to any property on the target entity and is not marked with <see cref="FilterIgnoreAttribute"/>.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool WarnOnUnmappedProperties { get; set; } = true;
}

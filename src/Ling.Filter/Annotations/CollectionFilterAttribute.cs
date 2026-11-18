namespace Ling.Filter.Annotations;

/// <summary>
/// Configures filtering over a child collection navigation property on the entity using <c>Any</c>, <c>All</c>, or <c>None</c>.
/// <para>
/// Example: <c>[CollectionFilter(nameof(Order.Items), nameof(OrderItem.SkuCode), StringMode = StringFilterMode.StartsWith)]</c>
/// generates <c>x =&gt; x.Items != null &amp;&amp; x.Items.Any(i =&gt; i.SkuCode != null &amp;&amp; i.SkuCode.StartsWith(val))</c>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class CollectionFilterAttribute : FilterBaseAttribute
{
    /// <summary>
    /// Gets the dot-separated path to the collection navigation property on the entity (for example, <c>"Orders"</c> or <c>"Department.Members"</c>).
    /// </summary>
    public string NavigationProperty { get; }

    /// <summary>
    /// Gets or sets the optional property name or dot-separated path on the collection element type.
    /// When <see langword="null"/> or empty, the collection element itself is compared directly (for primitive/scalar collections such as <c>List&lt;string&gt;</c>).
    /// </summary>
    public string? ElementProperty { get; set; }

    /// <summary>
    /// Gets or sets the collection quantifier (<see cref="CollectionQuantifier.Any"/>, <see cref="CollectionQuantifier.All"/>, or <see cref="CollectionQuantifier.None"/>).
    /// Defaults to <see cref="CollectionQuantifier.Any"/>.
    /// </summary>
    public CollectionQuantifier Quantifier { get; set; } = CollectionQuantifier.Any;

    /// <summary>
    /// Gets or sets the string matching mode used when the element target is a <see cref="string"/>.
    /// Defaults to <see cref="StringFilterMode.Equals"/>.
    /// </summary>
    public StringFilterMode StringMode { get; set; } = StringFilterMode.Equals;

    /// <summary>
    /// Gets or sets the relational comparison operator used when the element target is numeric, date, or comparable.
    /// Defaults to <see cref="CompareOperator.Equal"/>.
    /// </summary>
    public CompareOperator Operator { get; set; } = CompareOperator.Equal;

    /// <summary>
    /// Gets or sets a value indicating whether string inputs are trimmed before matching.
    /// </summary>
    public bool Trim { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether string comparisons normalize both sides with <c>ToLower()</c>.
    /// </summary>
    public bool CaseInsensitive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether non-nullable value-type filter inputs skip predicate generation when equal to <see langword="default"/>.
    /// </summary>
    public bool IgnoreDefaultValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionFilterAttribute"/> class for a primitive/scalar collection navigation property.
    /// </summary>
    /// <param name="navigationProperty">The collection navigation property path on the entity.</param>
    public CollectionFilterAttribute(string navigationProperty)
    {
        NavigationProperty = navigationProperty;
        Target = navigationProperty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionFilterAttribute"/> class for a complex collection navigation property and element path.
    /// </summary>
    /// <param name="navigationProperty">The collection navigation property path on the entity.</param>
    /// <param name="elementProperty">The property name or dot-separated path on each collection element.</param>
    public CollectionFilterAttribute(string navigationProperty, string elementProperty)
    {
        NavigationProperty = navigationProperty;
        ElementProperty = elementProperty;
        Target = navigationProperty;
    }
}

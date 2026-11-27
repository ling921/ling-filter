namespace Ling.Filter.Annotations;

/// <summary>
/// Maps a single filter property (such as a search keyword) to multiple entity properties or navigation paths,
/// combining the resulting sub-predicates with <see cref="CombineLogic.Or"/> (default) or <see cref="CombineLogic.And"/>.
/// <para>
/// Example: <c>[MultiFieldFilter(nameof(User.Name), nameof(User.Email), "Department.Name")]</c>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MultiFieldFilterAttribute : Attribute
{
    /// <summary>
    /// Gets the target entity property names or dot-separated navigation paths to search across.
    /// </summary>
    public string[] Fields { get; }

    /// <summary>
    /// Gets or sets the string matching mode applied when comparing string fields.
    /// Defaults to <see cref="StringFilterMode.Contains"/>.
    /// </summary>
    public StringFilterMode Mode { get; set; } = StringFilterMode.Contains;

    /// <summary>
    /// Gets or sets the logical operator used to combine the individual field checks.
    /// Defaults to <see cref="CombineLogic.Or"/>.
    /// </summary>
    public CombineLogic Logic { get; set; } = CombineLogic.Or;

    /// <summary>
    /// Gets or sets the empty/whitespace ignore policy for string inputs.
    /// Defaults to <see cref="StringIgnorePolicy.NullOrWhiteSpace"/>.
    /// </summary>
    public StringIgnorePolicy IgnorePolicy { get; set; } = StringIgnorePolicy.NullOrWhiteSpace;

    /// <summary>
    /// Gets or sets a value indicating whether leading and trailing whitespace should be trimmed from string inputs.
    /// </summary>
    public bool Trim { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether string checks should normalize both sides using <c>ToLower()</c>.
    /// </summary>
    public bool CaseInsensitive { get; set; }

    /// <summary>
    /// Gets or sets the relational comparison operator used when comparing non-string scalar fields.
    /// Defaults to <see cref="CompareOperator.Equal"/>.
    /// </summary>
    public CompareOperator Operator { get; set; } = CompareOperator.Equal;

    /// <summary>
    /// Gets or sets a value indicating whether non-nullable value-type properties skip predicate generation
    /// when their value equals <see langword="default"/>.
    /// </summary>
    public bool IgnoreDefaultValue { get; set; }

    /// <summary>
    /// Gets or sets an optional logical group name for combining this multi-field predicate with other grouped properties.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiFieldFilterAttribute"/> class with the specified target fields.
    /// </summary>
    /// <param name="fields">One or more entity property names or dot-separated navigation paths.</param>
    public MultiFieldFilterAttribute(params string[] fields)
    {
        Fields = fields ?? [];
    }
}

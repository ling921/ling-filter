namespace Ling.Filter.Annotations;

/// <summary>
/// Configures relational comparison (<c>==</c>, <c>!=</c>, <c>&gt;</c>, <c>&gt;=</c>, <c>&lt;</c>, <c>&lt;=</c>)
/// for numeric, date, enum, or comparable properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class CompareFilterAttribute : FilterBaseAttribute
{
    /// <summary>
    /// Gets or sets the relational operator used to compare the entity property against the filter property.
    /// </summary>
    public CompareOperator Operator { get; set; } = CompareOperator.Equal;

    /// <summary>
    /// Gets or sets a value indicating whether non-nullable value-type properties skip predicate generation
    /// when their value equals <see langword="default"/>.
    /// </summary>
    public bool IgnoreDefaultValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareFilterAttribute"/> class using <see cref="CompareOperator.Equal"/>.
    /// </summary>
    public CompareFilterAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareFilterAttribute"/> class using the specified <paramref name="operator"/>.
    /// </summary>
    /// <param name="operator">The comparison operator to apply.</param>
    public CompareFilterAttribute(CompareOperator @operator)
    {
        Operator = @operator;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareFilterAttribute"/> class using the specified <paramref name="operator"/> and <paramref name="target"/> path.
    /// </summary>
    /// <param name="operator">The comparison operator to apply.</param>
    /// <param name="target">The target property name or dot-separated navigation path on the entity.</param>
    public CompareFilterAttribute(CompareOperator @operator, string target)
    {
        Operator = @operator;
        Target = target;
    }
}

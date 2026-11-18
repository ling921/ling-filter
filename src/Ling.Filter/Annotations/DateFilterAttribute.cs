namespace Ling.Filter.Annotations;

/// <summary>
/// Configures precision-aware, index-friendly (Sargable) filtering for date and time properties
/// (<see cref="DateTime"/>, <see cref="DateTimeOffset"/>, <c>DateOnly</c>, and <c>TimeOnly</c>).
/// <para>
/// For example, <c>[DateFilter(DatePrecision.Day)]</c> with <see cref="CompareOperator.Equal"/> generates
/// <c>x.CreatedAt &gt;= startOfDay &amp;&amp; x.CreatedAt &lt; startOfNextDay</c>, preserving database index seek capability.
/// With <see cref="CompareOperator.LessThanOrEqual"/> and <see cref="DatePrecision.Day"/>, it generates <c>x.CreatedAt &lt; startOfNextDay</c>
/// so end-of-day filter inputs naturally include the entire calendar day.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DateFilterAttribute : FilterBaseAttribute
{
    /// <summary>
    /// Gets or sets the date/time truncation precision. Defaults to <see cref="DatePrecision.Day"/>.
    /// </summary>
    public DatePrecision Precision { get; set; } = DatePrecision.Day;

    /// <summary>
    /// Gets or sets the relational comparison operator. Defaults to <see cref="CompareOperator.Equal"/>.
    /// </summary>
    public CompareOperator Operator { get; set; } = CompareOperator.Equal;

    /// <summary>
    /// Gets or sets a value indicating whether non-nullable date/time properties skip predicate generation
    /// when their value equals <see langword="default"/>.
    /// </summary>
    public bool IgnoreDefaultValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateFilterAttribute"/> class with <see cref="DatePrecision.Day"/> equality.
    /// </summary>
    public DateFilterAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateFilterAttribute"/> class with the specified <paramref name="precision"/>.
    /// </summary>
    /// <param name="precision">The date/time truncation precision.</param>
    public DateFilterAttribute(DatePrecision precision)
    {
        Precision = precision;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateFilterAttribute"/> class with the specified <paramref name="precision"/> and <paramref name="target"/> path.
    /// </summary>
    /// <param name="precision">The date/time truncation precision.</param>
    /// <param name="target">The target property name or dot-separated navigation path on the entity.</param>
    public DateFilterAttribute(DatePrecision precision, string target)
    {
        Precision = precision;
        Target = target;
    }
}

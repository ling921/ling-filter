namespace Ling.Filter.Annotations;

/// <summary>
/// Configures boundary inclusivity and target mapping for a <see cref="ValueRange{T}"/> filter property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class RangeFilterAttribute : FilterBaseAttribute
{
    /// <summary>
    /// Gets or sets a value indicating whether the lower bound comparison uses <c>&gt;=</c> (<see langword="true"/>)
    /// or <c>&gt;</c> (<see langword="false"/>). Defaults to <see langword="true"/>.
    /// </summary>
    public bool MinInclusive { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the upper bound comparison uses <c>&lt;=</c> (<see langword="true"/>)
    /// or <c>&lt;</c> (<see langword="false"/>). Defaults to <see langword="true"/>.
    /// </summary>
    public bool MaxInclusive { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeFilterAttribute"/> class with closed bounds <c>[Min, Max]</c>.
    /// </summary>
    public RangeFilterAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeFilterAttribute"/> class targeting the specified entity property path.
    /// </summary>
    /// <param name="target">The target property name or dot-separated navigation path on the entity.</param>
    public RangeFilterAttribute(string target)
    {
        Target = target;
    }
}

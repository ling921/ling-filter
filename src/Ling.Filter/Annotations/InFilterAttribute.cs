namespace Ling.Filter.Annotations;

/// <summary>
/// Configures an <c>IN (...)</c> containment filter where a collection property on the filter model
/// matches entity rows whose scalar target property is contained in the collection (<c>values.Contains(x.Target)</c>).
/// <para>
/// When the filter collection is <see langword="null"/> or empty, the filter condition is skipped automatically.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class InFilterAttribute : FilterBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InFilterAttribute"/> class.
    /// </summary>
    public InFilterAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InFilterAttribute"/> class targeting the specified entity property path.
    /// </summary>
    /// <param name="target">The target property name or dot-separated navigation path on the entity.</param>
    public InFilterAttribute(string target)
    {
        Target = target;
    }
}

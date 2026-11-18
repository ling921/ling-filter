namespace Ling.Filter.Annotations;

/// <summary>
/// Configures a <c>NOT IN (...)</c> exclusion filter where a collection property on the filter model
/// matches entity rows whose scalar target property is not contained in the collection (<c>!values.Contains(x.Target)</c>).
/// <para>
/// When the filter collection is <see langword="null"/> or empty, the filter condition is skipped automatically.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class NotInFilterAttribute : FilterBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotInFilterAttribute"/> class.
    /// </summary>
    public NotInFilterAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotInFilterAttribute"/> class targeting the specified entity property path.
    /// </summary>
    /// <param name="target">The target property name or dot-separated navigation path on the entity.</param>
    public NotInFilterAttribute(string target)
    {
        Target = target;
    }
}

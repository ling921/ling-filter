namespace Ling.Filter.Annotations;

/// <summary>
/// Maps a filter property to a specific target property name or dot-separated navigation path on the entity.
/// <para>
/// For example: <c>[FilterField("Department.Name")]</c>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FilterFieldAttribute : FilterBaseAttribute
{
    /// <summary>
    /// Gets or sets a value indicating whether non-nullable value-type properties skip predicate generation
    /// when their value equals <see langword="default"/> (such as <c>0</c> or <see cref="Guid.Empty"/>).
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool IgnoreDefaultValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterFieldAttribute"/> class.
    /// </summary>
    public FilterFieldAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterFieldAttribute"/> class targeting the specified entity property path.
    /// </summary>
    /// <param name="target">The target property name or dot-separated navigation path on the entity.</param>
    public FilterFieldAttribute(string target)
    {
        Target = target;
    }
}

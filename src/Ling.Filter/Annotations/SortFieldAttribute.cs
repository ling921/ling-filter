namespace Ling.Filter.Annotations;

/// <summary>
/// Registers an additional or aliased sortable field on a filter model implementing <see cref="ISortable{TEntity}"/>,
/// or customizes how a property name maps to an entity sort path.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public sealed class SortFieldAttribute : Attribute
{
    /// <summary>
    /// Gets the external sort key name matched (case-insensitively) against <see cref="ISortable{TEntity}.SortBy"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the target property name or dot-separated navigation path on the entity.
    /// When <see langword="null"/> or empty, <see cref="Name"/> (or the annotated property's name) is used.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SortFieldAttribute"/> class with the specified sort key <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The external sort key name.</param>
    public SortFieldAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SortFieldAttribute"/> class mapping <paramref name="name"/> to <paramref name="target"/>.
    /// </summary>
    /// <param name="name">The external sort key name.</param>
    /// <param name="target">The target property name or dot-separated navigation path on the entity.</param>
    public SortFieldAttribute(string name, string target)
    {
        Name = name;
        Target = target;
    }
}

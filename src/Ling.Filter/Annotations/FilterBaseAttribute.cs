namespace Ling.Filter.Annotations;

/// <summary>
/// Serves as the base attribute for member-level filter annotations in <c>Ling.Filter</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public abstract class FilterBaseAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the target property name or dot-separated single-navigation path on the entity
    /// (for example, <c>"Name"</c> or <c>"Department.Manager.Name"</c>).
    /// When <see langword="null"/> or empty, the annotated DTO property name is used.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets an optional logical group name.
    /// <para>
    /// Properties sharing the same <see cref="Group"/> name are combined together first (using the group's configured
    /// <see cref="CombineLogic"/>, defaulting to <see cref="CombineLogic.Or"/>) and parenthesized before being combined
    /// with the remaining filter conditions via logical <c>AND</c>.
    /// </para>
    /// </summary>
    public string? Group { get; set; }
}

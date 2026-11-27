namespace Ling.Filter.Annotations;

/// <summary>
/// Configures the fallback sort field(s) and direction(s) used by <see cref="ISortable{TEntity}.ApplySort"/>
/// when <see cref="ISortable{TEntity}.SortBy"/> is <see langword="null"/>, whitespace, or does not match any allowed sort field.
/// <para>
/// May be applied multiple times to define secondary sort keys (<c>ThenBy</c> / <c>ThenByDescending</c>) in declaration order.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public sealed class DefaultSortAttribute : Attribute
{
    /// <summary>
    /// Gets the target entity property name or dot-separated navigation path used for default sorting.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the default sort order is descending.
    /// Defaults to <see langword="false"/> (ascending).
    /// </summary>
    public bool Descending { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSortAttribute"/> class targeting <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The target entity property name or dot-separated navigation path.</param>
    /// <param name="descending">Whether to sort in descending order by default.</param>
    public DefaultSortAttribute(string target, bool descending = false)
    {
        Target = target;
        Descending = descending;
    }
}

namespace Ling.Filter;

/// <summary>
/// Defines a strongly-typed dynamic sort contract for <typeparamref name="TEntity"/>.
/// <para>
/// Declare the implementing type as <see langword="partial"/> to allow the <c>Ling.Filter</c> source generator
/// to synthesize a reflection-free, injection-safe <see cref="ApplySort"/> implementation at compile time.
/// </para>
/// </summary>
/// <typeparam name="TEntity">The target entity type being sorted.</typeparam>
public interface ISortable<TEntity>
{
    /// <summary>
    /// Gets or sets the target sort field name or configured alias (matched case-insensitively).
    /// </summary>
    string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sorting should be applied in descending order.
    /// </summary>
    bool SortDescending { get; set; }

    /// <summary>
    /// Applies the requested sort specification (or configured default sort) onto the specified <paramref name="query"/>.
    /// </summary>
    /// <param name="query">The source <see cref="IQueryable{T}"/> to sort.</param>
    /// <returns>An ordered <see cref="IQueryable{T}"/> if a valid sort field or default sort is matched; otherwise, the original <paramref name="query"/>.</returns>
    IQueryable<TEntity> ApplySort(IQueryable<TEntity> query);
}

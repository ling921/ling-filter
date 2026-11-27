namespace Ling.Filter;

/// <summary>
/// Defines 1-based pagination parameters for query models.
/// <para>
/// Properties declared by <see cref="IPagedFilter"/> are automatically excluded from entity property filter generation.
/// </para>
/// </summary>
public interface IPagedFilter
{
    /// <summary>
    /// Gets the 1-based page number. Values less than <c>1</c> are normalized to <c>1</c> when paging is applied.
    /// </summary>
    int? Page { get; }

    /// <summary>
    /// Gets the maximum number of items per page. Paging is skipped when <see langword="null"/> or less than or equal to <c>0</c>.
    /// </summary>
    int? PageSize { get; }
}

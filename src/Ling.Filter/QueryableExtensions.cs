namespace Ling.Filter;

/// <summary>
/// Provides fluent extension methods on <see cref="IQueryable{T}"/> for applying strongly-typed filter, sort, and pagination models.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies the specified <paramref name="filter"/> model to the <paramref name="query"/>.
    /// If <paramref name="filter"/> is <see langword="null"/>, returns <paramref name="query"/> unchanged.
    /// </summary>
    /// <typeparam name="TEntity">The entity type of the query.</typeparam>
    /// <param name="query">The source <see cref="IQueryable{T}"/>.</param>
    /// <param name="filter">The filter model implementing <see cref="IFilterable{TEntity}"/>, or <see langword="null"/>.</param>
    /// <returns>The filtered <see cref="IQueryable{T}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is <see langword="null"/>.</exception>
    public static IQueryable<TEntity> ApplyFilter<TEntity>(
        this IQueryable<TEntity> query,
        IFilterable<TEntity>? filter)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return filter is null ? query : filter.Apply(query);
    }

    /// <summary>
    /// Applies the specified <paramref name="sortable"/> model to the <paramref name="query"/>.
    /// If <paramref name="sortable"/> is <see langword="null"/>, returns <paramref name="query"/> unchanged.
    /// </summary>
    /// <typeparam name="TEntity">The entity type of the query.</typeparam>
    /// <param name="query">The source <see cref="IQueryable{T}"/>.</param>
    /// <param name="sortable">The sort model implementing <see cref="ISortable{TEntity}"/>, or <see langword="null"/>.</param>
    /// <returns>The sorted <see cref="IQueryable{T}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is <see langword="null"/>.</exception>
    public static IQueryable<TEntity> ApplySort<TEntity>(
        this IQueryable<TEntity> query,
        ISortable<TEntity>? sortable)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return sortable is null ? query : sortable.ApplySort(query);
    }

    /// <summary>
    /// Applies 1-based pagination (<c>Skip</c> and <c>Take</c>) from <paramref name="paged"/> to the <paramref name="query"/>.
    /// If <paramref name="paged"/> is <see langword="null"/> or <see cref="IPagedFilter.PageSize"/> is not positive, returns <paramref name="query"/> unchanged.
    /// </summary>
    /// <typeparam name="TEntity">The entity type of the query.</typeparam>
    /// <param name="query">The source <see cref="IQueryable{T}"/>.</param>
    /// <param name="paged">The pagination model implementing <see cref="IPagedFilter"/>, or <see langword="null"/>.</param>
    /// <param name="maxPageSize">Optional upper bound to clamp <see cref="IPagedFilter.PageSize"/> when positive.</param>
    /// <returns>The paginated <see cref="IQueryable{T}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is <see langword="null"/>.</exception>
    public static IQueryable<TEntity> ApplyPaging<TEntity>(
        this IQueryable<TEntity> query,
        IPagedFilter? paged,
        int? maxPageSize = null)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (paged?.PageSize is not { } pageSize || pageSize <= 0)
        {
            return query;
        }

        if (maxPageSize is { } max && max > 0 && pageSize > max)
        {
            pageSize = max;
        }

        var page = paged.Page is { } p && p > 1 ? p : 1;
        var skipLong = (long)(page - 1) * pageSize;
        var skip = skipLong > int.MaxValue ? int.MaxValue : (int)skipLong;
        if (skip > 0)
        {
            query = query.Skip(skip);
        }

        return query.Take(pageSize);
    }

    /// <summary>
    /// Applies filtering, sorting (if <paramref name="queryModel"/> implements <see cref="ISortable{TEntity}"/>),
    /// and pagination (if <paramref name="queryModel"/> implements <see cref="IPagedFilter"/>) in standard execution order.
    /// </summary>
    /// <typeparam name="TEntity">The entity type of the query.</typeparam>
    /// <param name="query">The source <see cref="IQueryable{T}"/>.</param>
    /// <param name="queryModel">The query model implementing <see cref="IFilterable{TEntity}"/>, or <see langword="null"/>.</param>
    /// <param name="maxPageSize">Optional upper bound to clamp <see cref="IPagedFilter.PageSize"/> when pagination is applied.</param>
    /// <returns>The filtered, sorted, and paginated <see cref="IQueryable{T}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is <see langword="null"/>.</exception>
    public static IQueryable<TEntity> ApplyQuery<TEntity>(
        this IQueryable<TEntity> query,
        IFilterable<TEntity>? queryModel,
        int? maxPageSize = null)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (queryModel is null)
        {
            return query;
        }

        query = queryModel.Apply(query);

        if (queryModel is ISortable<TEntity> sortable)
        {
            query = sortable.ApplySort(query);
        }

        if (queryModel is IPagedFilter paged)
        {
            query = query.ApplyPaging(paged, maxPageSize);
        }

        return query;
    }
}

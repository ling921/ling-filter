using System.Linq.Expressions;

namespace Ling.Filter;

/// <summary>
/// Defines a strongly-typed filter model that can be converted into a LINQ predicate expression
/// or applied directly onto an <see cref="IQueryable{T}"/> sequence for <typeparamref name="TEntity"/>.
/// <para>
/// Declare the implementing type as <see langword="partial"/> to allow the <c>Ling.Filter</c> source generator
/// to synthesize zero-reflection <see cref="ToExpression"/> and <see cref="Apply"/> implementations at compile time.
/// </para>
/// </summary>
/// <typeparam name="TEntity">The target entity type being filtered.</typeparam>
public interface IFilterable<TEntity>
{
    /// <summary>
    /// Builds a combined predicate expression representing all active filter criteria on the current instance.
    /// <para>
    /// When no filter criteria are active, returns an expression equivalent to <c>x =&gt; true</c>.
    /// </para>
    /// </summary>
    /// <returns>
    /// An <see cref="Expression{TDelegate}"/> of type <c>Func&lt;TEntity, bool&gt;</c> suitable for EF Core SQL translation
    /// and in-memory evaluation.
    /// </returns>
    Expression<Func<TEntity, bool>> ToExpression();

    /// <summary>
    /// Applies all active filter criteria on the current instance sequentially onto the specified <paramref name="query"/>.
    /// </summary>
    /// <param name="query">The source <see cref="IQueryable{T}"/> to filter.</param>
    /// <returns>A filtered <see cref="IQueryable{T}"/> with active <c>Where</c> clauses appended.</returns>
    IQueryable<TEntity> Apply(IQueryable<TEntity> query);
}

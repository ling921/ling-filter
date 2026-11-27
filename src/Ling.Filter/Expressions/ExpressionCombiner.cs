using System.Linq.Expressions;
using Ling.Filter.Annotations;

namespace Ling.Filter.Expressions;

/// <summary>
/// Provides allocation-light, EF Core SQL-translatable combinators for <see cref="Expression{TDelegate}"/> predicates.
/// <para>
/// Unlike naive delegate invocation, <see cref="ExpressionCombiner"/> rebinds lambda parameters directly so the resulting
/// expression tree contains pure <c>AndAlso</c>, <c>OrElse</c>, and <c>Not</c> nodes with zero <see cref="InvocationExpression"/> overhead.
/// </para>
/// </summary>
public static class ExpressionCombiner
{
    /// <summary>
    /// Returns a constant <c>x =&gt; true</c> predicate expression for <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <returns>An expression equivalent to <c>x =&gt; true</c>.</returns>
    public static Expression<Func<TEntity, bool>> True<TEntity>() => static _ => true;

    /// <summary>
    /// Returns a constant <c>x =&gt; false</c> predicate expression for <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <returns>An expression equivalent to <c>x =&gt; false</c>.</returns>
    public static Expression<Func<TEntity, bool>> False<TEntity>() => static _ => false;

    /// <summary>
    /// Combines two optional predicate expressions using logical <c>AND</c> (<c>&amp;&amp;</c>).
    /// If either side is <see langword="null"/>, returns the other side without allocation.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <param name="left">The existing accumulated predicate, or <see langword="null"/>.</param>
    /// <param name="right">The new predicate to append, or <see langword="null"/>.</param>
    /// <returns>The combined predicate, or <see langword="null"/> if both inputs are <see langword="null"/>.</returns>
    public static Expression<Func<TEntity, bool>>? And<TEntity>(
        this Expression<Func<TEntity, bool>>? left,
        Expression<Func<TEntity, bool>>? right)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        var parameter = left.Parameters[0];
        var rightBody = ParameterRebinder.ReplaceParameter(right.Body, right.Parameters[0], parameter);
        return Expression.Lambda<Func<TEntity, bool>>(Expression.AndAlso(left.Body, rightBody), parameter);
    }

    /// <summary>
    /// Combines two optional predicate expressions using logical <c>OR</c> (<c>||</c>).
    /// If either side is <see langword="null"/>, returns the other side without allocation.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <param name="left">The existing accumulated predicate, or <see langword="null"/>.</param>
    /// <param name="right">The new predicate to append, or <see langword="null"/>.</param>
    /// <returns>The combined predicate, or <see langword="null"/> if both inputs are <see langword="null"/>.</returns>
    public static Expression<Func<TEntity, bool>>? Or<TEntity>(
        this Expression<Func<TEntity, bool>>? left,
        Expression<Func<TEntity, bool>>? right)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        var parameter = left.Parameters[0];
        var rightBody = ParameterRebinder.ReplaceParameter(right.Body, right.Parameters[0], parameter);
        return Expression.Lambda<Func<TEntity, bool>>(Expression.OrElse(left.Body, rightBody), parameter);
    }

    /// <summary>
    /// Combines two optional predicate expressions using the specified <paramref name="logic"/>.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <param name="left">The existing accumulated predicate, or <see langword="null"/>.</param>
    /// <param name="right">The new predicate to append, or <see langword="null"/>.</param>
    /// <param name="logic">The boolean combination operator.</param>
    /// <returns>The combined predicate, or <see langword="null"/> if both inputs are <see langword="null"/>.</returns>
    public static Expression<Func<TEntity, bool>>? Combine<TEntity>(
        this Expression<Func<TEntity, bool>>? left,
        Expression<Func<TEntity, bool>>? right,
        CombineLogic logic)
    {
        return logic == CombineLogic.Or ? Or(left, right) : And(left, right);
    }

    /// <summary>
    /// Negates the specified <paramref name="predicate"/> using logical <c>NOT</c> (<c>!</c>).
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <param name="predicate">The predicate expression to negate.</param>
    /// <returns>The negated predicate expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static Expression<Func<TEntity, bool>> Not<TEntity>(this Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return Expression.Lambda<Func<TEntity, bool>>(Expression.Not(predicate.Body), predicate.Parameters[0]);
    }
}

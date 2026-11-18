using System.Linq.Expressions;

namespace Ling.Filter.Expressions;

/// <summary>
/// Rebinds occurrences of a source <see cref="ParameterExpression"/> to a replacement <see cref="ParameterExpression"/>
/// so multiple lambda bodies can be merged without introducing <see cref="InvocationExpression"/> nodes.
/// </summary>
internal sealed class ParameterRebinder : ExpressionVisitor
{
    private readonly ParameterExpression _source;
    private readonly ParameterExpression _target;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterRebinder"/> class.
    /// </summary>
    /// <param name="source">The parameter expression to replace.</param>
    /// <param name="target">The replacement parameter expression.</param>
    public ParameterRebinder(ParameterExpression source, ParameterExpression target)
    {
        _source = source;
        _target = target;
    }

    /// <summary>
    /// Replaces all occurrences of <paramref name="source"/> in <paramref name="expression"/> with <paramref name="target"/>.
    /// </summary>
    /// <param name="expression">The expression tree to rewrite.</param>
    /// <param name="source">The parameter expression to replace.</param>
    /// <param name="target">The replacement parameter expression.</param>
    /// <returns>The rewritten expression tree.</returns>
    public static Expression ReplaceParameter(
        Expression expression,
        ParameterExpression source,
        ParameterExpression target)
    {
        return ReferenceEquals(source, target)
            ? expression
            : new ParameterRebinder(source, target).Visit(expression);
    }

    /// <inheritdoc/>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return ReferenceEquals(node, _source) ? _target : base.VisitParameter(node);
    }
}

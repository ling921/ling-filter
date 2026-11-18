namespace Ling.Filter.Annotations;

/// <summary>
/// Marks a parameterless instance method on a filter model that returns a custom
/// <c>Expression&lt;Func&lt;TEntity, bool&gt;&gt;?</c> escape-hatch predicate.
/// <para>
/// When the method returns a non-<see langword="null"/> expression at runtime, the generated <c>ToExpression()</c>
/// and <c>Apply(query)</c> methods automatically merge it with the property-based predicates via logical <c>AND</c>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class CustomFilterAttribute : Attribute
{
}

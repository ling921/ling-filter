namespace Ling.Filter.Annotations;

/// <summary>
/// Marks a parameterless instance method on a filter model that returns a custom
/// <c>Expression&lt;Func&lt;TEntity, bool&gt;&gt;?</c> escape-hatch predicate.
/// <para>
/// When the method returns a non-<see langword="null"/> expression at runtime, the generated <c>ToExpression()</c>
/// and <c>Apply(query)</c> methods automatically merge it with the property-based predicates (or into the specified <see cref="Group"/>).
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class CustomFilterAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an optional logical group name for combining this custom predicate with other grouped properties.
    /// </summary>
    public string? Group { get; set; }
}

namespace Ling.Filter.Annotations;

/// <summary>
/// Excludes a property from both automatic and attribute-based filter predicate generation and dynamic sorting.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FilterIgnoreAttribute : Attribute
{
}

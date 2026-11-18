namespace Ling.Filter.Annotations;

/// <summary>
/// Maps a <see cref="bool"/> or <c>bool?</c> filter property to a <c>!= null</c> or <c>== null</c> check on a nullable target entity property.
/// <para>
/// Example: <c>[NullCheckFilter(nameof(Order.ShippedAt))] public bool? IsShipped { get; set; }</c>
/// generates <c>x.ShippedAt != null</c> when <c>true</c>, <c>x.ShippedAt == null</c> when <c>false</c>, and is skipped when <c>null</c>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class NullCheckFilterAttribute : FilterBaseAttribute
{
    /// <summary>
    /// Gets or sets whether a <see langword="true"/> filter value checks for non-null (<see cref="NullCheckBehavior.IsNotNull"/>)
    /// or null (<see cref="NullCheckBehavior.IsNull"/>). Defaults to <see cref="NullCheckBehavior.IsNotNull"/>.
    /// </summary>
    public NullCheckBehavior Behavior { get; set; } = NullCheckBehavior.IsNotNull;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullCheckFilterAttribute"/> class targeting the specified entity property path.
    /// </summary>
    /// <param name="target">The nullable reference or nullable value-type property path on the entity.</param>
    public NullCheckFilterAttribute(string target)
    {
        Target = target;
    }
}

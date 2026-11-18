namespace Ling.Filter.Annotations;

/// <summary>
/// Assigns a filter property to a named logical group or configures how members of that group are combined.
/// <para>
/// All active filter properties sharing the same <see cref="Name"/> are combined together using <see cref="Logic"/>
/// (defaulting to <see cref="CombineLogic.Or"/>) inside parentheses, and the resulting group predicate is combined
/// with the remaining top-level predicates using logical <c>AND</c>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public sealed class FilterGroupAttribute : Attribute
{
    /// <summary>
    /// Gets the logical group identifier.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the boolean operator used to combine conditions inside this group.
    /// Defaults to <see cref="CombineLogic.Or"/>.
    /// </summary>
    public CombineLogic Logic { get; set; } = CombineLogic.Or;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterGroupAttribute"/> class for the specified group <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The logical group identifier.</param>
    public FilterGroupAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterGroupAttribute"/> class for the specified group <paramref name="name"/> and <paramref name="logic"/>.
    /// </summary>
    /// <param name="name">The logical group identifier.</param>
    /// <param name="logic">The boolean operator used to combine conditions inside this group.</param>
    public FilterGroupAttribute(string name, CombineLogic logic)
    {
        Name = name;
        Logic = logic;
    }
}

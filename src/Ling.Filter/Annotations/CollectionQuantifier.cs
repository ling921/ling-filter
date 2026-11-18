namespace Ling.Filter.Annotations;

/// <summary>
/// Specifies the quantifier applied to a child collection navigation property.
/// </summary>
public enum CollectionQuantifier
{
    /// <summary>
    /// Matches when at least one element in the child collection satisfies the predicate (<c>x.Items.Any(...)</c>).
    /// </summary>
    Any = 0,

    /// <summary>
    /// Matches when every element in the child collection satisfies the predicate (<c>x.Items.All(...)</c>).
    /// </summary>
    All = 1,

    /// <summary>
    /// Matches when no element in the child collection satisfies the predicate (<c>!x.Items.Any(...)</c>).
    /// </summary>
    None = 2,
}

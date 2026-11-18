namespace Ling.Filter.Annotations;

/// <summary>
/// Specifies the comparison mode used when filtering string properties.
/// </summary>
public enum StringFilterMode
{
    /// <summary>
    /// Matches when the target string equals the filter value (<c>x.Prop == value</c>).
    /// </summary>
    Equals = 0,

    /// <summary>
    /// Matches when the target string does not equal the filter value (<c>x.Prop != value</c>).
    /// </summary>
    NotEquals = 1,

    /// <summary>
    /// Matches when the target string contains the filter value (<c>x.Prop != null &amp;&amp; x.Prop.Contains(value)</c>).
    /// </summary>
    Contains = 2,

    /// <summary>
    /// Matches when the target string is <see langword="null"/> or does not contain the filter value (<c>x.Prop == null || !x.Prop.Contains(value)</c>).
    /// </summary>
    NotContains = 3,

    /// <summary>
    /// Matches when the target string starts with the filter value (<c>x.Prop != null &amp;&amp; x.Prop.StartsWith(value)</c>).
    /// </summary>
    StartsWith = 4,

    /// <summary>
    /// Matches when the target string ends with the filter value (<c>x.Prop != null &amp;&amp; x.Prop.EndsWith(value)</c>).
    /// </summary>
    EndsWith = 5,
}

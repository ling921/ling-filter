namespace Ling.Filter.Annotations;

/// <summary>
/// Specifies how a <see cref="FlagsAttribute"/> enum filter value is matched against the target enum property.
/// </summary>
public enum FlagsMatchMode
{
    /// <summary>
    /// Matches when the target value contains all specified flags (<c>(x.Flags &amp; value) == value</c>).
    /// </summary>
    HasAll = 0,

    /// <summary>
    /// Matches when the target value shares at least one bit flag with the filter value (<c>(x.Flags &amp; value) != 0</c>).
    /// </summary>
    HasAny = 1,

    /// <summary>
    /// Matches when the target value equals the exact bitmask of the filter value (<c>x.Flags == value</c>).
    /// </summary>
    Exact = 2,

    /// <summary>
    /// Matches when the target value shares no bit flags with the filter value (<c>(x.Flags &amp; value) == 0</c>).
    /// </summary>
    HasNone = 3,
}

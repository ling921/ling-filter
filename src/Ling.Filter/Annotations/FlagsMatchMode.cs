namespace Ling.Filter.Annotations;

/// <summary>
/// Specifies how a <see cref="FlagsAttribute"/> enum filter value is matched against the target enum property.
/// </summary>
public enum FlagsMatchMode
{
    /// <summary>
    /// Matches when the target value contains all specified flags (<c>x.Flags.HasFlag(value)</c>).
    /// </summary>
    HasAllFlags = 0,

    /// <summary>
    /// Matches when the target value shares at least one bit flag with the filter value (<c>(x.Flags &amp; value) != 0</c>).
    /// </summary>
    HasAnyFlag = 1,
}

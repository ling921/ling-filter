namespace Ling.Filter.Annotations;

/// <summary>
/// Configures string comparison behavior for a string filter property.
/// <para>
/// Supports <see cref="StringFilterMode.Equals"/>, <see cref="StringFilterMode.NotEquals"/>,
/// <see cref="StringFilterMode.Contains"/>, <see cref="StringFilterMode.NotContains"/>,
/// <see cref="StringFilterMode.StartsWith"/>, and <see cref="StringFilterMode.EndsWith"/>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class StringFilterAttribute : FilterBaseAttribute
{
    /// <summary>
    /// Gets or sets the string matching mode. Defaults to <see cref="StringFilterMode.Contains"/> when this attribute is applied without arguments.
    /// </summary>
    public StringFilterMode Mode { get; set; } = StringFilterMode.Contains;

    /// <summary>
    /// Gets or sets the empty/whitespace ignore policy for the input string.
    /// Defaults to <see cref="StringIgnorePolicy.NullOrWhiteSpace"/>.
    /// </summary>
    public StringIgnorePolicy IgnorePolicy { get; set; } = StringIgnorePolicy.NullOrWhiteSpace;

    /// <summary>
    /// Gets or sets a value indicating whether leading and trailing whitespace should be trimmed from the filter value before matching.
    /// </summary>
    public bool Trim { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether comparison should normalize both sides using <c>ToLower()</c> for case-insensitive database collations.
    /// </summary>
    public bool CaseInsensitive { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringFilterAttribute"/> class with <see cref="StringFilterMode.Contains"/>.
    /// </summary>
    public StringFilterAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringFilterAttribute"/> class with the specified matching mode.
    /// </summary>
    /// <param name="mode">The string matching mode to apply.</param>
    public StringFilterAttribute(StringFilterMode mode)
    {
        Mode = mode;
    }
}

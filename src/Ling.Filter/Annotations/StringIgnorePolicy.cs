namespace Ling.Filter.Annotations;

/// <summary>
/// Specifies when a string filter property should be considered inactive and skipped during predicate generation.
/// </summary>
public enum StringIgnorePolicy
{
    /// <summary>
    /// Skips the filter condition when <see cref="string.IsNullOrWhiteSpace(string?)"/> returns <see langword="true"/>.
    /// </summary>
    NullOrWhiteSpace = 0,

    /// <summary>
    /// Skips the filter condition when <see cref="string.IsNullOrEmpty(string?)"/> returns <see langword="true"/>.
    /// </summary>
    NullOrEmpty = 1,

    /// <summary>
    /// Skips the filter condition only when the string value is <see langword="null"/>.
    /// </summary>
    NullOnly = 2,
}

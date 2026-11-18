namespace Ling.Filter.Annotations;

/// <summary>
/// Specifies the relational comparison operator used for numeric, date, and comparable properties.
/// </summary>
public enum CompareOperator
{
    /// <summary>
    /// Equality comparison (<c>==</c>).
    /// </summary>
    Equal = 0,

    /// <summary>
    /// Inequality comparison (<c>!=</c>).
    /// </summary>
    NotEqual = 1,

    /// <summary>
    /// Strictly greater-than comparison (<c>&gt;</c>).
    /// </summary>
    GreaterThan = 2,

    /// <summary>
    /// Greater-than-or-equal comparison (<c>&gt;=</c>).
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    /// Strictly less-than comparison (<c>&lt;</c>).
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// Less-than-or-equal comparison (<c>&lt;=</c>).
    /// </summary>
    LessThanOrEqual = 5,
}

namespace Ling.Filter.Annotations;

/// <summary>
/// Specifies the boolean operator used to combine multiple sub-expressions.
/// </summary>
public enum CombineLogic
{
    /// <summary>
    /// Combines conditions with logical <c>AND</c> (<c>&amp;&amp;</c>).
    /// </summary>
    And = 0,

    /// <summary>
    /// Combines conditions with logical <c>OR</c> (<c>||</c>).
    /// </summary>
    Or = 1,
}

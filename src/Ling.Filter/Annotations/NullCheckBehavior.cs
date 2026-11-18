namespace Ling.Filter.Annotations;

/// <summary>
/// Specifies how a boolean filter value maps to <see langword="null"/> or non-<see langword="null"/> checks on a target property.
/// </summary>
public enum NullCheckBehavior
{
    /// <summary>
    /// When the filter property is <see langword="true"/>, matches <c>x.Target != null</c>;
    /// when <see langword="false"/>, matches <c>x.Target == null</c>.
    /// </summary>
    IsNotNull = 0,

    /// <summary>
    /// When the filter property is <see langword="true"/>, matches <c>x.Target == null</c>;
    /// when <see langword="false"/>, matches <c>x.Target != null</c>.
    /// </summary>
    IsNull = 1,
}

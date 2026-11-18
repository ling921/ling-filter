namespace Ling.Filter;

/// <summary>
/// Represents an optional lower and upper bound range for comparable value types such as numbers and dates.
/// <para>
/// When only <see cref="Min"/> is specified, only a lower-bound comparison is generated.
/// When only <see cref="Max"/> is specified, only an upper-bound comparison is generated.
/// When both are specified, both bounds are combined with logical <c>AND</c>.
/// </para>
/// </summary>
/// <typeparam name="T">The underlying value type of the range endpoints.</typeparam>
/// <param name="Min">The optional lower bound of the range.</param>
/// <param name="Max">The optional upper bound of the range.</param>
public readonly record struct ValueRange<T>(T? Min = null, T? Max = null)
    where T : struct
{
    /// <summary>
    /// Gets a value indicating whether either <see cref="Min"/> or <see cref="Max"/> has a value.
    /// </summary>
    public bool HasValue => Min.HasValue || Max.HasValue;

    /// <summary>
    /// Creates a range with only a minimum bound.
    /// </summary>
    /// <param name="min">The lower bound value.</param>
    /// <returns>A <see cref="ValueRange{T}"/> with <see cref="Min"/> populated.</returns>
    public static ValueRange<T> FromMin(T min) => new(Min: min, Max: null);

    /// <summary>
    /// Creates a range with only a maximum bound.
    /// </summary>
    /// <param name="max">The upper bound value.</param>
    /// <returns>A <see cref="ValueRange{T}"/> with <see cref="Max"/> populated.</returns>
    public static ValueRange<T> FromMax(T max) => new(Min: null, Max: max);

    /// <summary>
    /// Creates a range with both minimum and maximum bounds.
    /// </summary>
    /// <param name="min">The lower bound value.</param>
    /// <param name="max">The upper bound value.</param>
    /// <returns>A <see cref="ValueRange{T}"/> with both <see cref="Min"/> and <see cref="Max"/> populated.</returns>
    public static ValueRange<T> Between(T min, T max) => new(Min: min, Max: max);
}

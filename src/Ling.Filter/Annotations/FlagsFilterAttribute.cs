namespace Ling.Filter.Annotations;

/// <summary>
/// Configures bitwise flag matching (<see cref="FlagsMatchMode.HasAllFlags"/> or <see cref="FlagsMatchMode.HasAnyFlag"/>)
/// for enum properties decorated with <see cref="FlagsAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FlagsFilterAttribute : FilterBaseAttribute
{
    /// <summary>
    /// Gets or sets the bitwise flag matching mode. Defaults to <see cref="FlagsMatchMode.HasAllFlags"/>.
    /// </summary>
    public FlagsMatchMode Mode { get; set; } = FlagsMatchMode.HasAllFlags;

    /// <summary>
    /// Gets or sets a value indicating whether a zero/default enum value skips predicate generation.
    /// Defaults to <see langword="true"/> for bitwise flags.
    /// </summary>
    public bool IgnoreZero { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlagsFilterAttribute"/> class using <see cref="FlagsMatchMode.HasAllFlags"/>.
    /// </summary>
    public FlagsFilterAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlagsFilterAttribute"/> class using the specified <paramref name="mode"/>.
    /// </summary>
    /// <param name="mode">The bitwise flag matching mode.</param>
    public FlagsFilterAttribute(FlagsMatchMode mode)
    {
        Mode = mode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlagsFilterAttribute"/> class using the specified <paramref name="mode"/> and <paramref name="target"/> path.
    /// </summary>
    /// <param name="mode">The bitwise flag matching mode.</param>
    /// <param name="target">The target enum property name or dot-separated navigation path on the entity.</param>
    public FlagsFilterAttribute(FlagsMatchMode mode, string target)
    {
        Mode = mode;
        Target = target;
    }
}

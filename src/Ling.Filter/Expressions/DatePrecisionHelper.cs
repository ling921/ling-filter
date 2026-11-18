using Ling.Filter.Annotations;

namespace Ling.Filter.Expressions;

/// <summary>
/// Computes index-friendly (Sargable) interval boundaries for date and time values at a given <see cref="DatePrecision"/>.
/// </summary>
public static class DatePrecisionHelper
{
    /// <summary>
    /// Computes the half-open interval <c>[Start, End)</c> for a <see cref="DateTime"/> at the specified <paramref name="precision"/>,
    /// preserving <see cref="DateTime.Kind"/>.
    /// </summary>
    /// <param name="value">The input <see cref="DateTime"/> value.</param>
    /// <param name="precision">The desired truncation precision.</param>
    /// <returns>A tuple containing the inclusive <c>Start</c> and exclusive <c>End</c> boundaries.</returns>
    public static (DateTime Start, DateTime End) GetRange(DateTime value, DatePrecision precision)
    {
        var kind = value.Kind;
        switch (precision)
        {
            case DatePrecision.Year:
            {
                var start = new DateTime(value.Year, 1, 1, 0, 0, 0, kind);
                return (start, start.AddYears(1));
            }
            case DatePrecision.Month:
            {
                var start = new DateTime(value.Year, value.Month, 1, 0, 0, 0, kind);
                return (start, start.AddMonths(1));
            }
            case DatePrecision.Day:
            {
                var start = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, kind);
                return (start, start.AddDays(1));
            }
            case DatePrecision.Hour:
            {
                var start = new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0, kind);
                return (start, start.AddHours(1));
            }
            case DatePrecision.Minute:
            {
                var start = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, kind);
                return (start, start.AddMinutes(1));
            }
            case DatePrecision.Second:
            {
                var start = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, kind);
                return (start, start.AddSeconds(1));
            }
            default:
                return (value, value);
        }
    }

    /// <summary>
    /// Computes the half-open interval <c>[Start, End)</c> for a <see cref="DateTimeOffset"/> at the specified <paramref name="precision"/>,
    /// preserving <see cref="DateTimeOffset.Offset"/>.
    /// </summary>
    /// <param name="value">The input <see cref="DateTimeOffset"/> value.</param>
    /// <param name="precision">The desired truncation precision.</param>
    /// <returns>A tuple containing the inclusive <c>Start</c> and exclusive <c>End</c> boundaries.</returns>
    public static (DateTimeOffset Start, DateTimeOffset End) GetRange(DateTimeOffset value, DatePrecision precision)
    {
        var offset = value.Offset;
        switch (precision)
        {
            case DatePrecision.Year:
            {
                var start = new DateTimeOffset(value.Year, 1, 1, 0, 0, 0, offset);
                return (start, start.AddYears(1));
            }
            case DatePrecision.Month:
            {
                var start = new DateTimeOffset(value.Year, value.Month, 1, 0, 0, 0, offset);
                return (start, start.AddMonths(1));
            }
            case DatePrecision.Day:
            {
                var start = new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, offset);
                return (start, start.AddDays(1));
            }
            case DatePrecision.Hour:
            {
                var start = new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, 0, 0, offset);
                return (start, start.AddHours(1));
            }
            case DatePrecision.Minute:
            {
                var start = new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, offset);
                return (start, start.AddMinutes(1));
            }
            case DatePrecision.Second:
            {
                var start = new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, offset);
                return (start, start.AddSeconds(1));
            }
            default:
                return (value, value);
        }
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Computes the half-open interval <c>[Start, End)</c> for a <see cref="DateOnly"/> at the specified <paramref name="precision"/>.
    /// </summary>
    /// <param name="value">The input <see cref="DateOnly"/> value.</param>
    /// <param name="precision">The desired truncation precision.</param>
    /// <returns>A tuple containing the inclusive <c>Start</c> and exclusive <c>End</c> boundaries.</returns>
    public static (DateOnly Start, DateOnly End) GetRange(DateOnly value, DatePrecision precision)
    {
        switch (precision)
        {
            case DatePrecision.Year:
            {
                var start = new DateOnly(value.Year, 1, 1);
                return (start, start.AddYears(1));
            }
            case DatePrecision.Month:
            {
                var start = new DateOnly(value.Year, value.Month, 1);
                return (start, start.AddMonths(1));
            }
            case DatePrecision.Day:
            case DatePrecision.Hour:
            case DatePrecision.Minute:
            case DatePrecision.Second:
                return (value, value.AddDays(1));
            default:
                return (value, value);
        }
    }

    /// <summary>
    /// Computes the closed interval <c>[Start, EndInclusive]</c> for a <see cref="TimeOnly"/> at the specified <paramref name="precision"/>
    /// without wrapping around midnight (<c>23:59:59.9999999</c>).
    /// </summary>
    /// <param name="value">The input <see cref="TimeOnly"/> value.</param>
    /// <param name="precision">The desired truncation precision.</param>
    /// <returns>A tuple containing the inclusive <c>Start</c> and inclusive <c>EndInclusive</c> boundaries.</returns>
    public static (TimeOnly Start, TimeOnly EndInclusive) GetTimeRange(TimeOnly value, DatePrecision precision)
    {
        switch (precision)
        {
            case DatePrecision.Hour:
            {
                var start = new TimeOnly(value.Hour, 0, 0);
                var end = new TimeOnly(start.Ticks + TimeSpan.TicksPerHour - 1);
                return (start, end);
            }
            case DatePrecision.Minute:
            {
                var start = new TimeOnly(value.Hour, value.Minute, 0);
                var end = new TimeOnly(start.Ticks + TimeSpan.TicksPerMinute - 1);
                return (start, end);
            }
            case DatePrecision.Second:
            {
                var start = new TimeOnly(value.Hour, value.Minute, value.Second);
                var end = new TimeOnly(start.Ticks + TimeSpan.TicksPerSecond - 1);
                return (start, end);
            }
            case DatePrecision.Year:
            case DatePrecision.Month:
            case DatePrecision.Day:
                return (TimeOnly.MinValue, TimeOnly.MaxValue);
            default:
                return (value, value);
        }
    }
#endif
}

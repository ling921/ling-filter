namespace Ling.Filter.Annotations;

/// <summary>
/// Specifies the truncation precision for date and time filter comparisons.
/// <para>
/// Non-<see cref="Exact"/> precisions are expanded into index-friendly (Sargable) half-open range predicates
/// (<c>x.Date &gt;= start &amp;&amp; x.Date &lt; end</c>) rather than applying SQL functions to the column.
/// </para>
/// </summary>
public enum DatePrecision
{
    /// <summary>
    /// Compares the exact date/time value without truncation.
    /// </summary>
    Exact = 0,

    /// <summary>
    /// Truncates to the calendar year (<c>[YYYY-01-01, YYYY+1-01-01)</c>).
    /// </summary>
    Year = 1,

    /// <summary>
    /// Truncates to the calendar month (<c>[YYYY-MM-01, nextMonth)</c>).
    /// </summary>
    Month = 2,

    /// <summary>
    /// Truncates to the calendar day (<c>[YYYY-MM-DD 00:00:00, nextDay)</c>).
    /// </summary>
    Day = 3,

    /// <summary>
    /// Truncates to the hour (<c>[HH:00:00, HH+1:00:00)</c>).
    /// </summary>
    Hour = 4,

    /// <summary>
    /// Truncates to the minute (<c>[HH:mm:00, HH:mm+1:00)</c>).
    /// </summary>
    Minute = 5,

    /// <summary>
    /// Truncates sub-second ticks to the whole second (<c>[HH:mm:ss, HH:mm:ss+1)</c>).
    /// </summary>
    Second = 6,
}

using Ling.Filter.Test.Common;
using Ling.Filter.Test.Filters;

namespace Ling.Filter.Test;

public sealed class DateFilterTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task DateTime_AllPrecisions_GenerateSargableRangeSqlWithoutColumnFunctions()
    {
        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { ExactHiredAt = new DateTime(2025, 3, 15, 9, 30, 10) },
            1);

        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { HiredYear = new DateTime(2025, 8, 1) },
            1, 2);

        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { HiredMonth = new DateTime(2026, 11, 1) },
            3, 5);

        var daySql = await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { HiredDay = new DateTime(2025, 3, 15, 22, 11, 5) },
            1, 2);

        Assert.Contains(">=", daySql);
        Assert.Contains("<", daySql);
        Assert.DoesNotContain("strftime", daySql, StringComparison.OrdinalIgnoreCase);

        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { HiredHour = new DateTime(2025, 3, 15, 9, 0, 0) },
            1);

        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { HiredMinute = new DateTime(2026, 11, 18, 10, 20, 0) },
            3, 5);

        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { HiredSecond = new DateTime(2026, 11, 18, 10, 20, 45) },
            3);
    }

    [Fact]
    public async Task DateFilter_LessThanOrEqualDay_IncludesWholeEndDay_AndNotEqualHandlesNulls()
    {
        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { HiredThroughDay = new DateTime(2025, 3, 15) },
            1, 2, 4);

        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { NotTerminatedOnDay = new DateTime(2026, 11, 18) },
            1, 2, 4, 5);
    }

    [Fact]
    public async Task DateOnly_AndTimeOnly_IncludingMidnightHour_WorkWithoutWrapAroundBug()
    {
        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { BirthYearMonth = new DateOnly(1998, 5, 1) },
            1);

        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { ShiftHour = new TimeOnly(23, 10, 0) },
            3, 5);

        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionEmployeeFilter { ShiftSecond = new TimeOnly(23, 59, 59) },
            5);
    }
}

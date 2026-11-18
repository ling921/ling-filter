using Ling.Filter.Test.Common;
using Ling.Filter.Test.Filters;

namespace Ling.Filter.Test;

public sealed class RangeFilterTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task ValueRange_SupportsBothBounds_SingleLower_SingleUpper_AndEmpty()
    {
        await fixture.AssertEmployeeFilterAsync(new RangeEmployeeFilter(), 1, 2, 3, 4, 5);

        await fixture.AssertEmployeeFilterAsync(
            new RangeEmployeeFilter { SalaryRange = ValueRange<decimal>.Between(18500.50m, 26000.00m) },
            1, 2, 5);

        await fixture.AssertEmployeeFilterAsync(
            new RangeEmployeeFilter { SalaryRange = ValueRange<decimal>.FromMin(26000.00m) },
            2, 4);

        await fixture.AssertEmployeeFilterAsync(
            new RangeEmployeeFilter { SalaryRange = ValueRange<decimal>.FromMax(18500.50m) },
            1, 3);
    }

    [Fact]
    public async Task RangeFilter_RespectsExclusiveAndHalfOpenBoundaries()
    {
        await fixture.AssertEmployeeFilterAsync(
            new RangeEmployeeFilter { HalfOpenAge = ValueRange<int>.Between(28, 35) },
            1, 5);

        await fixture.AssertEmployeeFilterAsync(
            new RangeEmployeeFilter { OpenBonus = ValueRange<int>.Between(1, 5) },
            1, 5);
    }
}

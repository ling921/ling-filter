using Ling.Filter.Test.Common;
using Ling.Filter.Test.Filters;

namespace Ling.Filter.Test;

public sealed class NumericAndCompareFilterTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task MinMaxAndFromToNamingConventions_FilterInclusiveBounds()
    {
        await fixture.AssertEmployeeFilterAsync(
            new NumericConventionEmployeeFilter { MinAge = 28, MaxAge = 35 },
            1, 2, 5);

        await fixture.AssertEmployeeFilterAsync(
            new NumericConventionEmployeeFilter { SalaryFrom = 20000m, SalaryTo = 30000m },
            2, 5);
    }

    [Fact]
    public async Task StrictGreaterThan_LessThan_NotEqual_AndNullableTarget_WorkConsistently()
    {
        await fixture.AssertEmployeeFilterAsync(
            new NumericConventionEmployeeFilter { BonusGreaterThan = 2 },
            1, 2);

        await fixture.AssertEmployeeFilterAsync(
            new NumericConventionEmployeeFilter { AgeLessThan = 28 },
            3);

        await fixture.AssertEmployeeFilterAsync(
            new NumericConventionEmployeeFilter { ExcludeStatus = AccountStatus.Active },
            3, 4, 5);
    }

    [Fact]
    public async Task NonNullableValueType_WithIgnoreDefaultValue_SkipsZeroAndAppliesNonZero()
    {
        await fixture.AssertEmployeeFilterAsync(
            new NumericConventionEmployeeFilter { ExactAgeIgnoreZero = 0 },
            1, 2, 3, 4, 5);

        await fixture.AssertEmployeeFilterAsync(
            new NumericConventionEmployeeFilter { ExactAgeIgnoreZero = 41 },
            4);
    }
}

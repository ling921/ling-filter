using Ling.Filter.Test.Common;
using Ling.Filter.Test.Filters;

namespace Ling.Filter.Test;

public sealed class MultiFieldAndGroupFilterTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task MultiFieldFilter_OrSearch_MatchesAcrossEntityAndNestedNavigations()
    {
        await fixture.AssertEmployeeFilterAsync(
            new MultiFieldAndGroupEmployeeFilter { Keyword = "  globex  " },
            3);

        await fixture.AssertEmployeeFilterAsync(
            new MultiFieldAndGroupEmployeeFilter { Keyword = "ACME" },
            1, 2);

        await fixture.AssertEmployeeFilterAsync(
            new MultiFieldAndGroupEmployeeFilter { Keyword = "research" },
            5);
    }

    [Fact]
    public async Task MultiFieldFilter_AndLogic_RequiresAllTargetFieldsToMatch()
    {
        await fixture.AssertEmployeeFilterAsync(
            new MultiFieldAndGroupEmployeeFilter { MustAppearInBoth = "e" },
            1, 3, 5);
    }

    [Fact]
    public async Task FilterGroup_CombinesGroupedPropertiesWithOr_AndTopLevelWithAnd()
    {
        await fixture.AssertEmployeeFilterAsync(
            new MultiFieldAndGroupEmployeeFilter
            {
                NameStarts = "Diana",
                DeptCode = "ENG",
            },
            1, 2, 4);

        await fixture.AssertEmployeeFilterAsync(
            new MultiFieldAndGroupEmployeeFilter
            {
                NameStarts = "Diana",
                DeptCode = "ENG",
                Status = AccountStatus.Active,
            },
            1, 2);
    }
}

using Ling.Filter.Test.Common;
using Ling.Filter.Test.Filters;
using Microsoft.EntityFrameworkCore;

namespace Ling.Filter.Test;

public sealed class CustomFilterAndInheritanceTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task InheritedProperties_AndCustomFilterEscapeHatch_MergeCleanly()
    {
        await fixture.AssertEmployeeFilterAsync(
            new DerivedCustomEmployeeFilter
            {
                MinAge = 30,
                OnlyHighEarnersOrAdmins = true,
            },
            2, 4);

        await fixture.AssertEmployeeFilterAsync(
            new DerivedCustomEmployeeFilter
            {
                MinAge = 25,
                NameKeyword = "Evan",
                OnlyHighEarnersOrAdmins = false,
            },
            5);
    }

    [Fact]
    public async Task SingleFilterDto_CanTargetMultipleEntitiesSimultaneously()
    {
        var filter = new MultiEntityPersonFilter { MinAge = 30 };

        var activeIds = await fixture.DbContext.Employees
            .ApplyFilter(filter)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToArrayAsync();

        var archiveIds = await fixture.DbContext.EmployeeArchives
            .ApplyFilter(filter)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToArrayAsync();

        Assert.Equal(new[] { 2, 4, 5 }, activeIds);
        Assert.Equal(new[] { 102 }, archiveIds);
    }

    [Fact]
    public async Task ReadonlyRecordStructFilter_AndNullFilter_WorkExpectedly()
    {
        await fixture.AssertEmployeeFilterAsync(new ValueStructEmployeeFilter(3), 3);

        var allIds = await fixture.DbContext.Employees
            .ApplyFilter(null)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToArrayAsync();

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, allIds);
    }
}

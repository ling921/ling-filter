using Ling.Filter.Test.Common;
using Ling.Filter.Test.Filters;
using Microsoft.EntityFrameworkCore;

namespace Ling.Filter.Test;

public sealed class SortAndPagingTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task ApplySort_UsesDefaultSort_WhenSortByIsNullOrUnknown()
    {
        var defaultIds = await fixture.DbContext.Employees
            .ApplySort(new SortedPagedEmployeeQuery())
            .Select(e => e.Id)
            .ToArrayAsync();
        Assert.Equal(new[] { 4, 2, 5, 1, 3 }, defaultIds);

        var unknownIds = await fixture.DbContext.Employees
            .ApplySort(new SortedPagedEmployeeQuery { SortBy = "NonAllowedInjection; DROP TABLE" })
            .Select(e => e.Id)
            .ToArrayAsync();
        Assert.Equal(new[] { 4, 2, 5, 1, 3 }, unknownIds);
    }

    [Fact]
    public async Task ApplySort_SupportsCaseInsensitiveEntityProps_AndNavigationAliases()
    {
        var ageAsc = await fixture.DbContext.Employees
            .ApplySort(new SortedPagedEmployeeQuery { SortBy = "aGe", SortDescending = false })
            .Select(e => e.Id)
            .ToArrayAsync();
        Assert.Equal(new[] { 3, 1, 5, 2, 4 }, ageAsc);

        var deptFiltered = await fixture.DbContext.Employees
            .Where(e => e.DepartmentId != null)
            .ApplySort(new SortedPagedEmployeeQuery { SortBy = "DEPTNAME", SortDescending = true })
            .Select(e => e.Id)
            .ToArrayAsync();

        Assert.Equal(3, deptFiltered[0]);
        Assert.Equal(5, deptFiltered[1]);
    }

    [Fact]
    public async Task ApplyQuery_CombinesFilterSortAndPagingInOrder()
    {
        var queryModel = new SortedPagedEmployeeQuery
        {
            MinAge = 28,
            SortBy = "Age",
            SortDescending = false,
            Page = 2,
            PageSize = 2,
        };

        var ids = await fixture.DbContext.Employees
            .ApplyQuery(queryModel)
            .Select(e => e.Id)
            .ToArrayAsync();

        Assert.Equal(new[] { 2, 4 }, ids);

        queryModel.Page = 0;
        var firstPageIds = await fixture.DbContext.Employees
            .ApplyQuery(queryModel)
            .Select(e => e.Id)
            .ToArrayAsync();

        Assert.Equal(new[] { 1, 5 }, firstPageIds);
    }
}

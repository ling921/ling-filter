using Ling.Filter.Test.Common;
using Ling.Filter.Test.Filters;

namespace Ling.Filter.Test;

public sealed class CollectionAndInFilterTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task InAndNotIn_HandleArraysListsLazyEnumerablesEmptyCollectionsAndNullableTargets()
    {
        await fixture.AssertEmployeeFilterAsync(
            new CollectionEmployeeFilter
            {
                Statuses = [],
                DepartmentIds = [],
                ExcludeDepartmentIds = Array.Empty<int>(),
            },
            1, 2, 3, 4, 5);

        await fixture.AssertEmployeeFilterAsync(
            new CollectionEmployeeFilter
            {
                Statuses = [AccountStatus.Active, AccountStatus.Draft],
            },
            1, 2, 5);

        await fixture.AssertEmployeeFilterAsync(
            new CollectionEmployeeFilter
            {
                DepartmentIds = [10, 30],
            },
            1, 2, 5);

        await fixture.AssertEmployeeFilterAsync(
            new CollectionEmployeeFilter
            {
                ExcludeDepartmentIds = YieldIds(10, 20),
            },
            4, 5);
    }

    [Fact]
    public async Task CollectionFilter_SupportsAnyAllNoneAndContainmentOnChildEntities()
    {
        await fixture.AssertEmployeeFilterAsync(
            new CollectionEmployeeFilter { AnyTagPrefix = "cs" },
            1, 5);

        await fixture.AssertEmployeeFilterAsync(
            new CollectionEmployeeFilter { AllTagMinScore = 88 },
            1, 2, 4);

        await fixture.AssertEmployeeFilterAsync(
            new CollectionEmployeeFilter { WithoutTag = "csharp" },
            2, 3, 4);

        await fixture.AssertEmployeeFilterAsync(
            new CollectionEmployeeFilter { AnyTagIn = ["roslyn", "crm"] },
            1, 3);
    }

    private static IEnumerable<int> YieldIds(params int[] ids)
    {
        foreach (var id in ids)
        {
            yield return id;
        }
    }
}

using Ling.Filter.Test.Common;
using Ling.Filter.Test.Filters;

namespace Ling.Filter.Test;

public sealed class NullCheckAndFlagsFilterTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task NullCheckFilter_SupportsIsNotNullAndIsNullBehaviorsAcrossScalarAndNavigation()
    {
        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter { IsTerminated = true },
            3, 4);

        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter { IsTerminated = false },
            1, 2, 5);

        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter { MissingEmail = true },
            4);

        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter { MissingEmail = false },
            1, 2, 3, 5);

        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter { HasCompany = true },
            1, 2, 3);

        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter { HasCompany = false },
            4, 5);
    }

    [Fact]
    public async Task FlagsFilter_SupportsHasAllFlags_HasAnyFlag_IgnoreZero_AndNullableEnumTarget()
    {
        await fixture.AssertEmployeeFilterAsync(new NullAndFlagsEmployeeFilter(), 1, 2, 3, 4, 5);

        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter
            {
                RequiredPermissions = AccountPermissions.Read | AccountPermissions.Write,
            },
            1, 2);

        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter
            {
                AnyPermission = AccountPermissions.Export | AccountPermissions.Admin,
            },
            2, 5);

        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter
            {
                RequiredOptionalPermissions = AccountPermissions.Read,
            },
            1, 3);

        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter
            {
                ExactPermissions = AccountPermissions.Read | AccountPermissions.Write,
            },
            1);

        await fixture.AssertEmployeeFilterAsync(
            new NullAndFlagsEmployeeFilter
            {
                ForbiddenPermissions = AccountPermissions.Write | AccountPermissions.Export,
            },
            3, 4);
    }
}

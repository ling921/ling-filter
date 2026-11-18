using Ling.Filter.Test.Common;
using Ling.Filter.Test.Filters;

namespace Ling.Filter.Test;

public sealed class StringFilterTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task AutoMappedString_DefaultsToEquals_AndSkipsWhitespace()
    {
        await fixture.AssertEmployeeFilterAsync(new ExactTargetStringFilter { Name = "   " }, 1, 2, 3, 4, 5);
        await fixture.AssertEmployeeFilterAsync(new ExactTargetStringFilter { Name = "Alice Zhang" }, 1);
    }

    [Fact]
    public async Task Contains_WithTrim_MatchesTrimmedKeyword()
    {
        var sql = await fixture.AssertEmployeeFilterAsync(
            new StringEmployeeFilter { NameContains = "  Zhang  " },
            1);

        Assert.Contains("@__", sql);
    }

    [Fact]
    public async Task StartsWith_EndsWith_AndNotContains_WithCaseInsensitive_WorkInSqlAndMemory()
    {
        await fixture.AssertEmployeeFilterAsync(
            new StringEmployeeFilter { EmailPrefix = "charlie@" },
            3);

        await fixture.AssertEmployeeFilterAsync(
            new StringEmployeeFilter { NameSuffix = "WU" },
            3);

        await fixture.AssertEmployeeFilterAsync(
            new StringEmployeeFilter { EmailNotContains = "acme" },
            3, 4, 5);
    }

    [Fact]
    public async Task IgnorePolicies_DistinguishEmptyAndWhitespaceStrings()
    {
        await fixture.AssertEmployeeFilterAsync(
            new StringEmployeeFilter { RawNicknameNullOnly = "" },
            3);

        await fixture.AssertEmployeeFilterAsync(
            new StringEmployeeFilter { RawNicknameNullOrEmpty = "" },
            1, 2, 3, 4, 5);

        await fixture.AssertEmployeeFilterAsync(
            new StringEmployeeFilter { RawNicknameNullOrEmpty = "   " },
            4);
    }

    [Fact]
    public async Task DeepNestedNavigation_HandlesNullIntermediateReferencesSafely()
    {
        await fixture.AssertEmployeeFilterAsync(
            new StringEmployeeFilter { CompanyCountry = "CN" },
            1, 2);

        await fixture.AssertEmployeeFilterAsync(
            new StringEmployeeFilter { NotCompanyCountry = "CN" },
            3, 4, 5);
    }
}

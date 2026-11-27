using System.Linq.Expressions;
using Ling.Filter.Annotations;
using Ling.Filter.Expressions;
using Ling.Filter.Test.Common;
using Microsoft.EntityFrameworkCore;

namespace Ling.Filter.Test;

public sealed partial record RecordEmployeeQuery(
    [property: StringFilter(StringFilterMode.StartsWith, nameof(EmployeeEntity.Name))] string? NamePrefix = null,
    string? SortBy = null,
    bool SortDescending = false,
    int? Page = null,
    int? PageSize = null)
    : IFilterable<EmployeeEntity>, ISortable<EmployeeEntity>, IPagedFilter;

[FilterOptions(AutoMapSortableProperties = false)]
[SortField("dept", "Department.Name")]
[DefaultSort(nameof(EmployeeEntity.Status), Descending = false)]
[DefaultSort(nameof(EmployeeEntity.Age), Descending = true)]
public sealed partial class StrictMultiDefaultSortQuery : ISortable<EmployeeEntity>
{
    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
}

[FilterOptions(
    DefaultStringMode = StringFilterMode.Contains,
    CaseInsensitiveStringsByDefault = true,
    DefaultLogic = CombineLogic.Or)]
public sealed partial class TopLevelOrAndCaseInsensitiveFilter : IFilterable<EmployeeEntity>
{
    [FilterField(nameof(EmployeeEntity.Name))]
    public string? NameTerm { get; set; }

    public int? Age { get; set; }
}

[FilterGroup("Combo", CombineLogic.Or)]
public sealed partial class RangeAndCustomInOrGroupFilter : IFilterable<EmployeeEntity>
{
    [RangeFilter(nameof(EmployeeEntity.Salary), Group = "Combo")]
    public ValueRange<decimal>? SalaryBand { get; set; }

    [CustomFilter(Group = "Combo")]
    public Expression<Func<EmployeeEntity, bool>>? IncludeSuspendedCustom()
    {
        return IncludeSuspended ? e => e.Status == AccountStatus.Suspended : null;
    }

    [FilterIgnore]
    public bool IncludeSuspended { get; set; }
}

public sealed partial class DatePrecisionRangeAndCollectionOverloadFilter : IFilterable<EmployeeEntity>
{
    [RangeFilter(nameof(EmployeeEntity.HiredAt), Precision = DatePrecision.Day)]
    public ValueRange<DateTime>? HiredDayRange { get; set; }

    [CollectionFilter(nameof(EmployeeEntity.Tags), CollectionQuantifier.Any, nameof(EmployeeTagEntity.Label), StringMode = StringFilterMode.StartsWith)]
    public string? TagPrefix { get; set; }
}

public sealed class ApiV1EnhancementsTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task ReadOnlyRecordQuery_ImplementsFilterableSortableAndPaged_AndApplyPagingSupportsMaxPageSizeAndOverflowGuard()
    {
        var recordQuery = new RecordEmployeeQuery(
            NamePrefix: "A",
            SortBy: "Age",
            SortDescending: false,
            Page: 1,
            PageSize: 100);

        var matched = await fixture.DbContext.Employees
            .ApplyQuery(recordQuery, maxPageSize: 1)
            .Select(e => e.Id)
            .ToArrayAsync();

        // Alice Zhang (Id=1) starts with "A"
        Assert.Equal(new[] { 1 }, matched);

        // MaxPageSize clamps requested PageSize=50 down to 2
        var clamped = await fixture.DbContext.Employees
            .OrderBy(e => e.Id)
            .ApplyPaging(new RecordEmployeeQuery(Page: 1, PageSize: 50), maxPageSize: 2)
            .Select(e => e.Id)
            .ToArrayAsync();
        Assert.Equal(new[] { 1, 2 }, clamped);

        // Extreme Page value does not overflow int multiplication
        var overflowSafe = await fixture.DbContext.Employees
            .OrderBy(e => e.Id)
            .ApplyPaging(new RecordEmployeeQuery(Page: int.MaxValue, PageSize: 10))
            .Select(e => e.Id)
            .ToArrayAsync();
        Assert.Empty(overflowSafe);
    }

    [Fact]
    public async Task StrictSortWhitelist_DisablesAutoEntitySort_AndMultiLevelDefaultSortUsesThenBy()
    {
        // "Age" is NOT in explicit [SortField], so with AutoMapSortableProperties = false it falls back to multi-level [DefaultSort]:
        // Status ASC (Draft=0: Id 5; Active=1: Id 2(35), Id 1(28); Suspended=2: Id 3; Archived=3: Id 4), then Age DESC -> 5, 2, 1, 3, 4
        var fallbackOrder = await fixture.DbContext.Employees
            .ApplySort(new StrictMultiDefaultSortQuery { SortBy = "Age", SortDescending = false })
            .Select(e => e.Id)
            .ToArrayAsync();
        Assert.Equal(new[] { 5, 2, 1, 3, 4 }, fallbackOrder);

        // Explicit alias "dept" still works
        var deptOrder = await fixture.DbContext.Employees
            .Where(e => e.DepartmentId != null)
            .ApplySort(new StrictMultiDefaultSortQuery { SortBy = "dept", SortDescending = true })
            .Select(e => e.Id)
            .ToArrayAsync();
        Assert.Equal(3, deptOrder[0]);
    }

    [Fact]
    public async Task FilterOptions_TopLevelOr_AndCaseInsensitiveDefault_AndFilterFieldInheritsDefaultStringMode()
    {
        // NameTerm="ALICE" (case-insensitive Contains via FilterField + FilterOptions) OR Age == 41 (Diana, Id=4)
        await fixture.AssertEmployeeFilterAsync(
            new TopLevelOrAndCaseInsensitiveFilter
            {
                NameTerm = "ALICE",
                Age = 41,
            },
            1, 4);
    }

    [Fact]
    public async Task RangeAndCustomFilter_InsideOrGroup_PreserveAtomicRangeAndCustomGrouping()
    {
        // Salary in [25000, 30000] (Bob=26000, Id=2) OR Suspended (charlie, Id=3).
        // Previously Range was split into (Salary >= 25000 || Salary <= 30000) which matched everyone!
        await fixture.AssertEmployeeFilterAsync(
            new RangeAndCustomInOrGroupFilter
            {
                SalaryBand = ValueRange<decimal>.Between(25000m, 30000m),
                IncludeSuspended = true,
            },
            2, 3);
    }

    [Fact]
    public async Task RangeFilter_WithDatePrecisionDay_CoversFullEndDay_AndThreeArgCollectionFilterWorks()
    {
        // HiredAt values in fixture:
        // Id 1: 2025-03-15 09:30:10, Id 2: 2025-03-15 14:45:00
        // Passing Max = 2025-03-15 00:00:00 with Precision = DatePrecision.Day must include both Id 1 and Id 2!
        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionRangeAndCollectionOverloadFilter
            {
                HiredDayRange = ValueRange<DateTime>.Between(
                    new DateTime(2025, 3, 15, 0, 0, 0),
                    new DateTime(2025, 3, 15, 0, 0, 0)),
            },
            1, 2);

        // 3-arg CollectionFilter(NavigationProperty, Quantifier, ElementProperty)
        await fixture.AssertEmployeeFilterAsync(
            new DatePrecisionRangeAndCollectionOverloadFilter
            {
                TagPrefix = "cs",
            },
            1, 5);
    }

    [Fact]
    public void ExpressionCombiner_ExtensionMethods_SupportFluentChaining()
    {
        Expression<Func<EmployeeEntity, bool>> isYoung = e => e.Age < 25;
        Expression<Func<EmployeeEntity, bool>> isSenior = e => e.Age >= 40;

        var combined = isYoung.Or(isSenior)!.Not();
        var ids = fixture.DbContext.Employees.Where(combined).OrderBy(e => e.Id).Select(e => e.Id).ToArray();
        // Excludes charlie (22, Id=3) and Diana (41, Id=4) -> leaves 1, 2, 5
        Assert.Equal(new[] { 1, 2, 5 }, ids);
    }
}

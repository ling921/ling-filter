using Ling.Filter.Test.Common;
using Ling.Filter.Test.Filters;

namespace Ling.Filter.Test;

public sealed class FilterOptionsAndAdvancedEdgeCaseTests(SqliteTestFixture fixture) : IClassFixture<SqliteTestFixture>
{
    [Fact]
    public async Task FilterOptions_OverridesDefaultStringMode_Trim_AndDisablesNamingConventions()
    {
        // Name uses Contains + Trim; MinAge = 99 is ignored because EnableNamingConventions = false
        await fixture.AssertEmployeeFilterAsync(
            new ConfiguredOptionsEmployeeFilter
            {
                Name = "  Zhang  ",
                MinAge = 99,
            },
            1);
    }

    [Fact]
    public void AdvancedDocumentFilter_CoversScalarMultiField_DateRelationalOperators_AndPrimitiveCollections()
    {
        var offset = TimeSpan.FromHours(8);
        var docs = new[]
        {
            new DocumentItem
            {
                Id = 1,
                PrimaryCode = 100,
                SecondaryCode = 200,
                UpdatedAt = new DateTimeOffset(2026, 11, 17, 23, 59, 0, offset),
                ScheduledAt = new TimeOnly(9, 30, 0),
                Labels = ["Urgent-Doc", "Finance"],
            },
            new DocumentItem
            {
                Id = 2,
                PrimaryCode = 300,
                SecondaryCode = 100,
                UpdatedAt = new DateTimeOffset(2026, 11, 18, 12, 0, 0, offset),
                ScheduledAt = new TimeOnly(10, 0, 0),
                Labels = ["Public-Note"],
            },
            new DocumentItem
            {
                Id = 3,
                PrimaryCode = 400,
                SecondaryCode = 500,
                UpdatedAt = new DateTimeOffset(2026, 11, 19, 0, 0, 0, offset),
                ScheduledAt = new TimeOnly(11, 15, 0),
                Labels = ["urgent-alert"],
            },
        };

        // Scalar MultiFieldFilter (PrimaryCode == 100 || SecondaryCode == 100)
        var byCode = docs.AsQueryable().ApplyFilter(new AdvancedDocumentFilter { AnyCode = 100 }).Select(d => d.Id).ToArray();
        Assert.Equal(new[] { 1, 2 }, byCode);

        // DatePrecision.Day GreaterThan 2026-11-18 -> >= 2026-11-19 00:00
        var afterNov18 = docs.Where(new AdvancedDocumentFilter
        {
            UpdatedAfterDay = new DateTimeOffset(2026, 11, 18, 8, 0, 0, offset),
        }.ToExpression().Compile()).Select(d => d.Id).ToArray();
        Assert.Equal(new[] { 3 }, afterNov18);

        // DatePrecision.Day GreaterThanOrEqual 2026-11-18 -> >= 2026-11-18 00:00
        var fromNov18 = docs.Where(new AdvancedDocumentFilter
        {
            UpdatedFromDay = new DateTimeOffset(2026, 11, 18, 22, 0, 0, offset),
        }.ToExpression().Compile()).Select(d => d.Id).ToArray();
        Assert.Equal(new[] { 2, 3 }, fromNov18);

        // DatePrecision.Day LessThan 2026-11-18 -> < 2026-11-18 00:00
        var beforeNov18 = docs.Where(new AdvancedDocumentFilter
        {
            UpdatedBeforeDay = new DateTimeOffset(2026, 11, 18, 15, 0, 0, offset),
        }.ToExpression().Compile()).Select(d => d.Id).ToArray();
        Assert.Equal(new[] { 1 }, beforeNov18);

        // TimeOnly GreaterThan Hour 9 -> > 09:59:59.9999999 (matches 10:00 and 11:15)
        var afterHour9 = docs.Where(new AdvancedDocumentFilter
        {
            ScheduledAfterHour = new TimeOnly(9, 0, 0),
        }.ToExpression().Compile()).Select(d => d.Id).ToArray();
        Assert.Equal(new[] { 2, 3 }, afterHour9);

        // Primitive List<string> CollectionFilter StartsWith case-insensitive
        var urgentDocs = docs.Where(new AdvancedDocumentFilter
        {
            LabelPrefix = "URGENT-",
        }.ToExpression().Compile()).Select(d => d.Id).ToArray();
        Assert.Equal(new[] { 1, 3 }, urgentDocs);
    }
}

using System.Linq.Expressions;
using Ling.Filter.Annotations;
using Ling.Filter.Expressions;

namespace Ling.Filter.Test;

public sealed class ExpressionAndHelperUnitTests
{
    [Fact]
    public void ExpressionCombiner_CombinesAndNegatesWithoutInvocationNodes()
    {
        var t = ExpressionCombiner.True<int>().Compile();
        var f = ExpressionCombiner.False<int>().Compile();
        Assert.True(t(42));
        Assert.False(f(42));

        Expression<Func<int, bool>> gt10 = x => x > 10;
        Expression<Func<int, bool>> lt20 = y => y < 20;

        Assert.Same(gt10, ExpressionCombiner.And(gt10, null));
        Assert.Same(lt20, ExpressionCombiner.And(null, lt20));
        Assert.Null(ExpressionCombiner.And<int>(null, null));

        var andExpr = ExpressionCombiner.Combine(gt10, lt20, CombineLogic.And)!;
        var orExpr = ExpressionCombiner.Combine<int>(x => x < 5, y => y > 50, CombineLogic.Or)!;
        var notExpr = ExpressionCombiner.Not(gt10);

        Assert.DoesNotContain("Invoke", andExpr.ToString());
        Assert.True(andExpr.Compile()(15));
        Assert.False(andExpr.Compile()(25));
        Assert.True(orExpr.Compile()(2));
        Assert.True(orExpr.Compile()(60));
        Assert.False(orExpr.Compile()(20));
        Assert.True(notExpr.Compile()(5));

        Assert.Throws<ArgumentNullException>(() => ExpressionCombiner.Not<int>(null!));
    }

    [Fact]
    public void DatePrecisionHelper_ComputesExactBoundariesForDateTimeOffsetDateOnlyAndTimeOnly()
    {
        var offset = TimeSpan.FromHours(8);
        var dto = new DateTimeOffset(2026, 11, 18, 13, 45, 29, 500, offset);

        Assert.Equal(
            (new DateTimeOffset(2026, 1, 1, 0, 0, 0, offset), new DateTimeOffset(2027, 1, 1, 0, 0, 0, offset)),
            DatePrecisionHelper.GetRange(dto, DatePrecision.Year));

        Assert.Equal(
            (new DateTimeOffset(2026, 11, 1, 0, 0, 0, offset), new DateTimeOffset(2026, 12, 1, 0, 0, 0, offset)),
            DatePrecisionHelper.GetRange(dto, DatePrecision.Month));

        Assert.Equal(
            (new DateTimeOffset(2026, 11, 18, 0, 0, 0, offset), new DateTimeOffset(2026, 11, 19, 0, 0, 0, offset)),
            DatePrecisionHelper.GetRange(dto, DatePrecision.Day));

        Assert.Equal(
            (new DateTimeOffset(2026, 11, 18, 13, 0, 0, offset), new DateTimeOffset(2026, 11, 18, 14, 0, 0, offset)),
            DatePrecisionHelper.GetRange(dto, DatePrecision.Hour));

        Assert.Equal(
            (new DateTimeOffset(2026, 11, 18, 13, 45, 0, offset), new DateTimeOffset(2026, 11, 18, 13, 46, 0, offset)),
            DatePrecisionHelper.GetRange(dto, DatePrecision.Minute));

        Assert.Equal(
            (new DateTimeOffset(2026, 11, 18, 13, 45, 29, offset), new DateTimeOffset(2026, 11, 18, 13, 45, 30, offset)),
            DatePrecisionHelper.GetRange(dto, DatePrecision.Second));

        var dateOnly = new DateOnly(2026, 2, 28);
        Assert.Equal((new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 1)), DatePrecisionHelper.GetRange(dateOnly, DatePrecision.Day));

        var lateTime = new TimeOnly(23, 59, 59, 123);
        var (tStart, tEnd) = DatePrecisionHelper.GetTimeRange(lateTime, DatePrecision.Hour);
        Assert.Equal(new TimeOnly(23, 0, 0), tStart);
        Assert.Equal(TimeOnly.MaxValue, tEnd);
    }

    [Fact]
    public void QueryableExtensions_ThrowArgumentNullException_WhenQueryIsNull()
    {
        IQueryable<int> nullQuery = null!;
        Assert.Throws<ArgumentNullException>(() => nullQuery.ApplyFilter(null));
        Assert.Throws<ArgumentNullException>(() => nullQuery.ApplySort(null));
        Assert.Throws<ArgumentNullException>(() => nullQuery.ApplyPaging(null));
        Assert.Throws<ArgumentNullException>(() => nullQuery.ApplyQuery(null));
    }
}

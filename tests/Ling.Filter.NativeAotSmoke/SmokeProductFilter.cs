using Ling.Filter.Annotations;

namespace Ling.Filter.NativeAotSmoke;

public sealed partial class SmokeProductFilter : IFilterable<SmokeProduct>, ISortable<SmokeProduct>, IPagedFilter
{
    [StringFilter(StringFilterMode.Contains, Trim = true)]
    public string? Name { get; set; }

    public ValueRange<decimal>? Price { get; set; }

    [DateFilter(DatePrecision.Day)]
    public DateTime? CreatedAt { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }

    public int? Page { get; set; }

    public int? PageSize { get; set; }
}

using Ling.Filter.Annotations;

namespace Ling.Filter.Sample.Contracts.Models;

/// <summary>
/// Demonstrates cross-assembly declarative filter, dynamic sort, and pagination generation for <see cref="Order"/>.
/// </summary>
[DefaultSort(nameof(Order.CreatedAt), Descending = true)]
[SortField("customerName", "Customer.Name")]
public sealed partial class OrderFilter : IFilterable<Order>, ISortable<Order>, IPagedFilter
{
    [MultiFieldFilter(nameof(Order.OrderNo), "Customer.Name", "Customer.Email", Trim = true)]
    public string? Keyword { get; set; }

    public ValueRange<decimal>? TotalAmount { get; set; }

    [DateFilter(DatePrecision.Day, nameof(Order.CreatedAt))]
    public DateTime? CreatedDate { get; set; }

    public OrderStatus[]? Statuses { get; set; }

    [CollectionFilter(nameof(Order.Items), nameof(OrderItem.SkuCode), StringMode = StringFilterMode.StartsWith)]
    public string? ItemSkuPrefix { get; set; }

    [NullCheckFilter(nameof(Order.ShippedAt))]
    public bool? IsShipped { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }

    public int? Page { get; set; }

    public int? PageSize { get; set; }
}

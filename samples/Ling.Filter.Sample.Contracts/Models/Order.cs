namespace Ling.Filter.Sample.Contracts.Models;

/// <summary>
/// Represents a sales order entity.
/// </summary>
public sealed class Order
{
    public int Id { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}

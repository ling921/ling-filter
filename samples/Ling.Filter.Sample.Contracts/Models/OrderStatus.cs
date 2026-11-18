namespace Ling.Filter.Sample.Contracts.Models;

/// <summary>
/// Represents the lifecycle status of a sample order.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Shipped = 2,
    Cancelled = 3,
}

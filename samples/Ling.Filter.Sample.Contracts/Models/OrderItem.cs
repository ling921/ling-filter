namespace Ling.Filter.Sample.Contracts.Models;

/// <summary>
/// Represents an item line within an <see cref="Order"/>.
/// </summary>
public sealed class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string SkuCode { get; set; } = string.Empty;

    public int Quantity { get; set; }
}

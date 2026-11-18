namespace Ling.Filter.Sample.Contracts.Models;

/// <summary>
/// Represents a customer placing orders.
/// </summary>
public sealed class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? City { get; set; }
}

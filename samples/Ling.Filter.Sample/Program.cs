using Ling.Filter;
using Ling.Filter.Sample;
using Ling.Filter.Sample.Contracts.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using var connection = new SqliteConnection("DataSource=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<SampleDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new SampleDbContext(options);
await db.Database.EnsureCreatedAsync();

var alice = new Customer { Name = "Alice Chen", Email = "alice@example.com", City = "Shanghai" };
var bob = new Customer { Name = "Bob Wang", Email = "bob@example.com", City = "Beijing" };

db.Orders.AddRange(
    new Order
    {
        OrderNo = "ORD-2026-001",
        TotalAmount = 299.50m,
        Status = OrderStatus.Shipped,
        CreatedAt = new DateTime(2026, 11, 18, 9, 30, 0),
        ShippedAt = new DateTime(2026, 11, 18, 12, 0, 0),
        Customer = alice,
        Items = [new OrderItem { SkuCode = "PRO-KB-01", Quantity = 1 }],
    },
    new Order
    {
        OrderNo = "ORD-2026-002",
        TotalAmount = 89.00m,
        Status = OrderStatus.Paid,
        CreatedAt = new DateTime(2026, 11, 18, 10, 15, 0),
        Customer = bob,
        Items = [new OrderItem { SkuCode = "STD-MS-02", Quantity = 2 }],
    });

await db.SaveChangesAsync();

var filter = new OrderFilter
{
    Keyword = "Alice",
    TotalAmount = ValueRange<decimal>.Between(100m, 500m),
    CreatedDate = new DateTime(2026, 11, 18),
    Statuses = [OrderStatus.Paid, OrderStatus.Shipped],
    ItemSkuPrefix = "PRO-",
    IsShipped = true,
    SortBy = "customerName",
    Page = 1,
    PageSize = 10,
};

var query = db.Orders.Include(o => o.Customer).ApplyQuery(filter);
Console.WriteLine("=== Generated Parameterized SQL ===");
Console.WriteLine(query.ToQueryString());

var results = await query.ToListAsync();
Console.WriteLine($"=== Matched Orders ({results.Count}) ===");
foreach (var order in results)
{
    Console.WriteLine($"{order.OrderNo} | {order.Customer?.Name} | {order.TotalAmount:C}");
}

using Ling.Filter.Sample.Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace Ling.Filter.Sample;

/// <summary>
/// In-memory SQLite DbContext for demonstrating SQL translation of generated filter predicates.
/// </summary>
public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
}

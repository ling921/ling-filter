using Microsoft.EntityFrameworkCore;

namespace Ling.Filter.Test.Common;

public sealed class FilterTestDbContext(DbContextOptions<FilterTestDbContext> options) : DbContext(options)
{
    public DbSet<CompanyEntity> Companies => Set<CompanyEntity>();

    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();

    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();

    public DbSet<EmployeeTagEntity> EmployeeTags => Set<EmployeeTagEntity>();

    public DbSet<EmployeeArchiveEntity> EmployeeArchives => Set<EmployeeArchiveEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeEntity>()
            .Property(e => e.Salary)
            .HasConversion<double>();
    }
}

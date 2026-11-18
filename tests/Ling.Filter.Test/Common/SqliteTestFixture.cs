using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Ling.Filter.Test.Common;

public sealed class SqliteTestFixture : IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    public FilterTestDbContext DbContext { get; private set; } = null!;

    public IReadOnlyList<EmployeeEntity> InMemoryEmployees { get; private set; } = [];

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FilterTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new FilterTestDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();

        var acme = new CompanyEntity { Id = 1, Title = "Acme Corp", Country = "CN" };
        var globex = new CompanyEntity { Id = 2, Title = "Globex Inc", Country = null };

        var eng = new DepartmentEntity { Id = 10, Name = "Engineering", Code = "ENG", Company = acme };
        var sales = new DepartmentEntity { Id = 20, Name = "Sales", Code = "SLS", Company = globex };
        var orphanDept = new DepartmentEntity { Id = 30, Name = "Research", Code = null, Company = null };

        var employees = new List<EmployeeEntity>
        {
            new()
            {
                Id = 1,
                Name = "Alice Zhang",
                Email = "alice@acme.com",
                Nickname = "Ali",
                Age = 28,
                Salary = 18500.50m,
                BonusLevel = 3,
                Status = AccountStatus.Active,
                Permissions = AccountPermissions.Read | AccountPermissions.Write,
                OptionalPermissions = AccountPermissions.Read | AccountPermissions.Export,
                HiredAt = new DateTime(2025, 3, 15, 9, 30, 10),
                TerminatedAt = null,
                BirthDate = new DateOnly(1998, 5, 20),
                ShiftStart = new TimeOnly(9, 15, 30),
                Department = eng,
                Tags =
                [
                    new EmployeeTagEntity { Label = "csharp", Score = 95 },
                    new EmployeeTagEntity { Label = "roslyn", Score = 90 },
                ],
            },
            new()
            {
                Id = 2,
                Name = "Bob Li",
                Email = "bob@acme.com",
                Nickname = null,
                Age = 35,
                Salary = 26000.00m,
                BonusLevel = 5,
                Status = AccountStatus.Active,
                Permissions = AccountPermissions.Read | AccountPermissions.Write | AccountPermissions.Admin,
                OptionalPermissions = null,
                HiredAt = new DateTime(2025, 3, 15, 14, 45, 0),
                TerminatedAt = null,
                BirthDate = new DateOnly(1991, 5, 28),
                ShiftStart = new TimeOnly(14, 0, 0),
                Department = eng,
                Tags =
                [
                    new EmployeeTagEntity { Label = "efcore", Score = 88 },
                ],
            },
            new()
            {
                Id = 3,
                Name = "charlie wu",
                Email = "CHARLIE@GLOBEX.COM",
                Nickname = "",
                Age = 22,
                Salary = 9500.00m,
                BonusLevel = null,
                Status = AccountStatus.Suspended,
                Permissions = AccountPermissions.Read,
                OptionalPermissions = AccountPermissions.Read,
                HiredAt = new DateTime(2026, 11, 18, 10, 20, 45),
                TerminatedAt = new DateTime(2026, 11, 18, 18, 0, 0),
                BirthDate = new DateOnly(2004, 11, 2),
                ShiftStart = new TimeOnly(23, 45, 59),
                Department = sales,
                Tags =
                [
                    new EmployeeTagEntity { Label = "sales-pro", Score = 70 },
                    new EmployeeTagEntity { Label = "crm", Score = 65 },
                ],
            },
            new()
            {
                Id = 4,
                Name = "Diana Chen",
                Email = null,
                Nickname = "   ",
                Age = 41,
                Salary = 32000.00m,
                BonusLevel = 1,
                Status = AccountStatus.Archived,
                Permissions = AccountPermissions.None,
                OptionalPermissions = AccountPermissions.Admin,
                HiredAt = new DateTime(2024, 12, 31, 23, 59, 59),
                TerminatedAt = new DateTime(2026, 1, 10, 12, 0, 0),
                BirthDate = new DateOnly(1985, 1, 15),
                ShiftStart = new TimeOnly(0, 0, 0),
                Department = null,
                Tags = [],
            },
            new()
            {
                Id = 5,
                Name = "Evan Zhao",
                Email = "evan@research.org",
                Nickname = "Ev",
                Age = 30,
                Salary = 21000.00m,
                BonusLevel = 2,
                Status = AccountStatus.Draft,
                Permissions = AccountPermissions.Read | AccountPermissions.Export,
                OptionalPermissions = null,
                HiredAt = new DateTime(2026, 11, 18, 10, 20, 59),
                TerminatedAt = null,
                BirthDate = new DateOnly(1996, 8, 9),
                ShiftStart = new TimeOnly(23, 59, 59),
                Department = orphanDept,
                Tags =
                [
                    new EmployeeTagEntity { Label = "csharp", Score = 82 },
                ],
            },
        };

        DbContext.Companies.AddRange(acme, globex);
        DbContext.Departments.AddRange(eng, sales, orphanDept);
        DbContext.Employees.AddRange(employees);
        DbContext.EmployeeArchives.AddRange(
            new EmployeeArchiveEntity { Id = 101, Name = "Alice Zhang", Age = 28 },
            new EmployeeArchiveEntity { Id = 102, Name = "Old Staff", Age = 55 });

        await DbContext.SaveChangesAsync();

        InMemoryEmployees = await DbContext.Employees
            .Include(e => e.Department)
            .ThenInclude(d => d!.Company)
            .Include(e => e.Tags)
            .OrderBy(e => e.Id)
            .ToListAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public async Task<string> AssertEmployeeFilterAsync(
        IFilterable<EmployeeEntity> filter,
        params int[] expectedIds)
    {
        var applyQuery = DbContext.Employees.ApplyFilter(filter).OrderBy(e => e.Id);
        var sql = applyQuery.ToQueryString();
        var applyIds = await applyQuery.Select(e => e.Id).ToArrayAsync();

        var exprIds = await DbContext.Employees
            .Where(filter.ToExpression())
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToArrayAsync();

        var compiled = filter.ToExpression().Compile();
        var memoryIds = InMemoryEmployees
            .Where(compiled)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToArray();

        Assert.Equal(expectedIds, applyIds);
        Assert.Equal(expectedIds, exprIds);
        Assert.Equal(expectedIds, memoryIds);

        return sql;
    }
}

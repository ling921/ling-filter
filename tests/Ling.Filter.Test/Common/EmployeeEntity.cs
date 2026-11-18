namespace Ling.Filter.Test.Common;

public sealed class EmployeeEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Nickname { get; set; }

    public int Age { get; set; }

    public decimal Salary { get; set; }

    public int? BonusLevel { get; set; }

    public AccountStatus Status { get; set; }

    public AccountPermissions Permissions { get; set; }

    public AccountPermissions? OptionalPermissions { get; set; }

    public DateTime HiredAt { get; set; }

    public DateTime? TerminatedAt { get; set; }

    public DateOnly BirthDate { get; set; }

    public TimeOnly ShiftStart { get; set; }

    public int? DepartmentId { get; set; }

    public DepartmentEntity? Department { get; set; }

    public List<EmployeeTagEntity> Tags { get; set; } = [];
}

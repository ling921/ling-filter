namespace Ling.Filter.Test.Common;

public sealed class EmployeeTagEntity
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string Label { get; set; } = string.Empty;

    public int Score { get; set; }
}

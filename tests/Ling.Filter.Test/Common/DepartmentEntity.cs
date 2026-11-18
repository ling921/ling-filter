namespace Ling.Filter.Test.Common;

public sealed class DepartmentEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public int? CompanyId { get; set; }

    public CompanyEntity? Company { get; set; }
}

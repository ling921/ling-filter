using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public sealed partial class CollectionEmployeeFilter : IFilterable<EmployeeEntity>
{
    public AccountStatus[]? Statuses { get; set; }

    public List<int>? DepartmentIds { get; set; }

    [NotInFilter(nameof(EmployeeEntity.DepartmentId))]
    public IEnumerable<int>? ExcludeDepartmentIds { get; set; }

    [CollectionFilter(nameof(EmployeeEntity.Tags), nameof(EmployeeTagEntity.Label), StringMode = StringFilterMode.StartsWith)]
    public string? AnyTagPrefix { get; set; }

    [CollectionFilter(nameof(EmployeeEntity.Tags), nameof(EmployeeTagEntity.Score), Quantifier = CollectionQuantifier.All, Operator = CompareOperator.GreaterThanOrEqual)]
    public int? AllTagMinScore { get; set; }

    [CollectionFilter(nameof(EmployeeEntity.Tags), nameof(EmployeeTagEntity.Label), Quantifier = CollectionQuantifier.None, StringMode = StringFilterMode.Equals)]
    public string? WithoutTag { get; set; }

    [CollectionFilter(nameof(EmployeeEntity.Tags), nameof(EmployeeTagEntity.Label))]
    public string[]? AnyTagIn { get; set; }
}

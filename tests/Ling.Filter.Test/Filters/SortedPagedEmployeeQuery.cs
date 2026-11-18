using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

[DefaultSort(nameof(EmployeeEntity.Salary), Descending = true)]
[SortField("deptName", "Department.Name")]
public sealed partial class SortedPagedEmployeeQuery : IFilterable<EmployeeEntity>, ISortable<EmployeeEntity>, IPagedFilter
{
    public int? MinAge { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }

    public int? Page { get; set; }

    public int? PageSize { get; set; }
}

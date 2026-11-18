using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

[FilterGroup("IdentityOrDept", CombineLogic.Or)]
public sealed partial class MultiFieldAndGroupEmployeeFilter : IFilterable<EmployeeEntity>
{
    [MultiFieldFilter(
        nameof(EmployeeEntity.Name),
        nameof(EmployeeEntity.Email),
        "Department.Name",
        "Department.Company.Title",
        Trim = true,
        CaseInsensitive = true)]
    public string? Keyword { get; set; }

    [MultiFieldFilter(
        nameof(EmployeeEntity.Name),
        "Department.Name",
        Mode = StringFilterMode.Contains,
        Logic = CombineLogic.And,
        CaseInsensitive = true)]
    public string? MustAppearInBoth { get; set; }

    [FilterGroup("IdentityOrDept")]
    [StringFilter(StringFilterMode.StartsWith, Target = nameof(EmployeeEntity.Name))]
    public string? NameStarts { get; set; }

    [FilterGroup("IdentityOrDept")]
    [FilterField("Department.Code")]
    public string? DeptCode { get; set; }

    public AccountStatus? Status { get; set; }
}

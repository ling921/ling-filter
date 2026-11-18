using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public sealed partial class StringEmployeeFilter : IFilterable<EmployeeEntity>
{
    [StringFilter(StringFilterMode.Equals, Target = nameof(EmployeeEntity.Name))]
    public string? ExactName { get; set; }

    [FilterIgnore]
    public string? Unused { get; set; }

    [StringFilter(StringFilterMode.NotEquals, Target = nameof(EmployeeEntity.Nickname))]
    public string? NotNickname { get; set; }

    [StringFilter(StringFilterMode.Contains, Target = nameof(EmployeeEntity.Name), Trim = true)]
    public string? NameContains { get; set; }

    [StringFilter(StringFilterMode.NotContains, Target = nameof(EmployeeEntity.Email), CaseInsensitive = true)]
    public string? EmailNotContains { get; set; }

    [StringFilter(StringFilterMode.StartsWith, Target = nameof(EmployeeEntity.Email), CaseInsensitive = true)]
    public string? EmailPrefix { get; set; }

    [StringFilter(StringFilterMode.EndsWith, Target = nameof(EmployeeEntity.Name), CaseInsensitive = true)]
    public string? NameSuffix { get; set; }

    [StringFilter(StringFilterMode.Equals, Target = nameof(EmployeeEntity.Nickname), IgnorePolicy = StringIgnorePolicy.NullOnly)]
    public string? RawNicknameNullOnly { get; set; }

    [StringFilter(StringFilterMode.Equals, Target = nameof(EmployeeEntity.Nickname), IgnorePolicy = StringIgnorePolicy.NullOrEmpty)]
    public string? RawNicknameNullOrEmpty { get; set; }

    [StringFilter(StringFilterMode.Equals, Target = "Department.Company.Country")]
    public string? CompanyCountry { get; set; }

    [StringFilter(StringFilterMode.NotEquals, Target = "Department.Company.Country")]
    public string? NotCompanyCountry { get; set; }
}

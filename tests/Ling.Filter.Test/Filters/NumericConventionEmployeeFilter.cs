using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public sealed partial class NumericConventionEmployeeFilter : IFilterable<EmployeeEntity>
{
    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public decimal? SalaryFrom { get; set; }

    public decimal? SalaryTo { get; set; }

    [CompareFilter(CompareOperator.GreaterThan, nameof(EmployeeEntity.BonusLevel))]
    public int? BonusGreaterThan { get; set; }

    [CompareFilter(CompareOperator.LessThan, nameof(EmployeeEntity.Age))]
    public int? AgeLessThan { get; set; }

    [CompareFilter(CompareOperator.NotEqual, nameof(EmployeeEntity.Status))]
    public AccountStatus? ExcludeStatus { get; set; }

    [FilterField(nameof(EmployeeEntity.Age), IgnoreDefaultValue = true)]
    public int ExactAgeIgnoreZero { get; set; }
}

using System.Linq.Expressions;
using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public sealed partial class DerivedCustomEmployeeFilter : BaseAgeFilter, IFilterable<EmployeeEntity>
{
    [FilterIgnore]
    public bool OnlyHighEarnersOrAdmins { get; set; }

    [StringFilter(StringFilterMode.Contains, Target = nameof(EmployeeEntity.Name))]
    public string? NameKeyword { get; set; }

    [CustomFilter]
    public Expression<Func<EmployeeEntity, bool>>? BuildSpecialRule()
    {
        if (!OnlyHighEarnersOrAdmins)
        {
            return null;
        }

        return e => e.Salary >= 25000m || ((int)e.Permissions & (int)AccountPermissions.Admin) != 0;
    }
}

using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public sealed partial class NullAndFlagsEmployeeFilter : IFilterable<EmployeeEntity>
{
    [NullCheckFilter(nameof(EmployeeEntity.TerminatedAt))]
    public bool? IsTerminated { get; set; }

    [NullCheckFilter(nameof(EmployeeEntity.Email), Behavior = NullCheckBehavior.IsNull)]
    public bool? MissingEmail { get; set; }

    [NullCheckFilter("Department.Company")]
    public bool? HasCompany { get; set; }

    [FlagsFilter(FlagsMatchMode.HasAll, nameof(EmployeeEntity.Permissions))]
    public AccountPermissions? RequiredPermissions { get; set; }

    [FlagsFilter(FlagsMatchMode.HasAny, nameof(EmployeeEntity.Permissions))]
    public AccountPermissions AnyPermission { get; set; }

    [FlagsFilter(FlagsMatchMode.Exact, nameof(EmployeeEntity.Permissions))]
    public AccountPermissions? ExactPermissions { get; set; }

    [FlagsFilter(FlagsMatchMode.HasNone, nameof(EmployeeEntity.Permissions))]
    public AccountPermissions? ForbiddenPermissions { get; set; }

    [FlagsFilter(FlagsMatchMode.HasAll, nameof(EmployeeEntity.OptionalPermissions))]
    public AccountPermissions? RequiredOptionalPermissions { get; set; }
}

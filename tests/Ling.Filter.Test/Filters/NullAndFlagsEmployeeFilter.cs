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

    [FlagsFilter(FlagsMatchMode.HasAllFlags, nameof(EmployeeEntity.Permissions))]
    public AccountPermissions? RequiredPermissions { get; set; }

    [FlagsFilter(FlagsMatchMode.HasAnyFlag, nameof(EmployeeEntity.Permissions))]
    public AccountPermissions AnyPermission { get; set; }

    [FlagsFilter(FlagsMatchMode.HasAllFlags, nameof(EmployeeEntity.OptionalPermissions))]
    public AccountPermissions? RequiredOptionalPermissions { get; set; }
}

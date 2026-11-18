using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public sealed partial record MultiEntityPersonFilter : IFilterable<EmployeeEntity>, IFilterable<EmployeeArchiveEntity>
{
    [StringFilter(StringFilterMode.Contains)]
    public string? Name { get; set; }

    public int? MinAge { get; set; }
}

using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public sealed partial class ExactTargetStringFilter : IFilterable<EmployeeEntity>
{
    public string? Name { get; set; }
}

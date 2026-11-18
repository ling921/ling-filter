using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public sealed partial class RangeEmployeeFilter : IFilterable<EmployeeEntity>
{
    public ValueRange<decimal>? SalaryRange { get; set; }

    [RangeFilter(nameof(EmployeeEntity.Age), MinInclusive = true, MaxInclusive = false)]
    public ValueRange<int> HalfOpenAge { get; set; }

    [RangeFilter(nameof(EmployeeEntity.BonusLevel), MinInclusive = false, MaxInclusive = false)]
    public ValueRange<int>? OpenBonus { get; set; }
}

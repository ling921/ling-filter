using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public sealed partial class DatePrecisionEmployeeFilter : IFilterable<EmployeeEntity>
{
    [DateFilter(DatePrecision.Exact, nameof(EmployeeEntity.HiredAt))]
    public DateTime? ExactHiredAt { get; set; }

    [DateFilter(DatePrecision.Year, nameof(EmployeeEntity.HiredAt))]
    public DateTime? HiredYear { get; set; }

    [DateFilter(DatePrecision.Month, nameof(EmployeeEntity.HiredAt))]
    public DateTime? HiredMonth { get; set; }

    [DateFilter(DatePrecision.Day, nameof(EmployeeEntity.HiredAt))]
    public DateTime? HiredDay { get; set; }

    [DateFilter(DatePrecision.Hour, nameof(EmployeeEntity.HiredAt))]
    public DateTime? HiredHour { get; set; }

    [DateFilter(DatePrecision.Minute, nameof(EmployeeEntity.HiredAt))]
    public DateTime? HiredMinute { get; set; }

    [DateFilter(DatePrecision.Second, nameof(EmployeeEntity.HiredAt))]
    public DateTime? HiredSecond { get; set; }

    [DateFilter(DatePrecision.Day, nameof(EmployeeEntity.HiredAt), Operator = CompareOperator.LessThanOrEqual)]
    public DateTime? HiredThroughDay { get; set; }

    [DateFilter(DatePrecision.Day, nameof(EmployeeEntity.TerminatedAt), Operator = CompareOperator.NotEqual)]
    public DateTime? NotTerminatedOnDay { get; set; }

    [DateFilter(DatePrecision.Month, nameof(EmployeeEntity.BirthDate))]
    public DateOnly? BirthYearMonth { get; set; }

    [DateFilter(DatePrecision.Hour, nameof(EmployeeEntity.ShiftStart))]
    public TimeOnly? ShiftHour { get; set; }

    [DateFilter(DatePrecision.Second, nameof(EmployeeEntity.ShiftStart))]
    public TimeOnly? ShiftSecond { get; set; }
}

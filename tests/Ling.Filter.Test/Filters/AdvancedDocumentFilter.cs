using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public sealed partial class AdvancedDocumentFilter : IFilterable<DocumentItem>
{
    [MultiFieldFilter(nameof(DocumentItem.PrimaryCode), nameof(DocumentItem.SecondaryCode))]
    public int? AnyCode { get; set; }

    [DateFilter(DatePrecision.Day, nameof(DocumentItem.UpdatedAt), Operator = CompareOperator.GreaterThan)]
    public DateTimeOffset? UpdatedAfterDay { get; set; }

    [DateFilter(DatePrecision.Day, nameof(DocumentItem.UpdatedAt), Operator = CompareOperator.GreaterThanOrEqual)]
    public DateTimeOffset? UpdatedFromDay { get; set; }

    [DateFilter(DatePrecision.Day, nameof(DocumentItem.UpdatedAt), Operator = CompareOperator.LessThan)]
    public DateTimeOffset? UpdatedBeforeDay { get; set; }

    [DateFilter(DatePrecision.Hour, nameof(DocumentItem.ScheduledAt), Operator = CompareOperator.GreaterThan)]
    public TimeOnly? ScheduledAfterHour { get; set; }

    [CollectionFilter(nameof(DocumentItem.Labels), StringMode = StringFilterMode.StartsWith, CaseInsensitive = true)]
    public string? LabelPrefix { get; set; }
}

namespace Ling.Filter.Test.Common;

public sealed class DocumentItem
{
    public int Id { get; set; }

    public int PrimaryCode { get; set; }

    public int SecondaryCode { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public TimeOnly ScheduledAt { get; set; }

    public List<string> Labels { get; set; } = [];
}

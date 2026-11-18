using Ling.Filter.Annotations;
using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

[FilterOptions(
    AutoMapSameNameProperties = true,
    EnableNamingConventions = false,
    DefaultStringMode = StringFilterMode.Contains,
    DefaultStringIgnorePolicy = StringIgnorePolicy.NullOrEmpty,
    TrimStringsByDefault = true,
    WarnOnUnmappedProperties = false)]
public sealed partial class ConfiguredOptionsEmployeeFilter : IFilterable<EmployeeEntity>
{
    // Auto-mapped with DefaultStringMode = Contains and TrimStringsByDefault = true
    public string? Name { get; set; }

    // Unmapped because EnableNamingConventions = false, and no LF0005 warning because WarnOnUnmappedProperties = false
    public int? MinAge { get; set; }
}

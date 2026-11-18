namespace Ling.Filter.Generators.Models;

/// <summary>
/// Categorizes the kind of predicate code to emit for a filter property or method.
/// </summary>
internal enum FilterRuleKind
{
    String = 0,
    Compare = 1,
    Range = 2,
    Date = 3,
    MultiField = 4,
    In = 5,
    NotIn = 6,
    Collection = 7,
    NullCheck = 8,
    Flags = 9,
    CustomMethod = 10,
}

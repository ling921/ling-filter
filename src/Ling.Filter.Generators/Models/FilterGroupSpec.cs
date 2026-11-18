namespace Ling.Filter.Generators.Models;

/// <summary>
/// Value-equatable configuration for a named logical filter group.
/// </summary>
/// <param name="Name">The group identifier.</param>
/// <param name="CombineLogicValue">0 for <c>And</c>, 1 for <c>Or</c>.</param>
internal sealed record FilterGroupSpec(
    string Name,
    int CombineLogicValue);

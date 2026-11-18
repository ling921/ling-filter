using Ling.Filter.Generators.Infrastructure;

namespace Ling.Filter.Generators.Models;

/// <summary>
/// Value-equatable sequence of <see cref="PropertySegmentSpec"/> hops representing a resolved entity path.
/// </summary>
/// <param name="Segments">Ordered hops from the root parameter to the leaf target property.</param>
internal sealed record PropertyPathSpec(
    EquatableArray<PropertySegmentSpec> Segments);

using System.Collections.Immutable;

namespace Ling.Filter.Generators.Infrastructure;

/// <summary>
/// Factory extensions for <see cref="EquatableArray{T}"/>.
/// </summary>
internal static class EquatableArrayExtensions
{
    public static EquatableArray<T> AsEquatableArray<T>(this ImmutableArray<T> array)
        where T : IEquatable<T>
    {
        return new(array);
    }

    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> collection)
        where T : IEquatable<T>
    {
        return new(collection);
    }
}

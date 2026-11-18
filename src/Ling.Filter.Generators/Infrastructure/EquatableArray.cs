using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ling.Filter.Generators.Infrastructure;

/// <summary>
/// An immutable array wrapper providing structural value equality for Roslyn incremental generator caching.
/// </summary>
/// <typeparam name="T">The equatable element type.</typeparam>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[]? _array;

    public EquatableArray(ImmutableArray<T> array)
    {
        _array = Unsafe.As<ImmutableArray<T>, T[]?>(ref array);
    }

    public EquatableArray(IEnumerable<T> collection)
    {
        _array = collection as T[] ?? collection.ToArray();
    }

    public int Length => _array?.Length ?? 0;

    public bool IsEmpty => Length == 0;

    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref AsImmutableArray().ItemRef(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T> AsImmutableArray()
    {
        return Unsafe.As<T[]?, ImmutableArray<T>>(ref Unsafe.AsRef(in _array));
    }

    public ReadOnlySpan<T> AsSpan() => AsImmutableArray().AsSpan();

    public bool Equals(EquatableArray<T> other) => AsSpan().SequenceEqual(other.AsSpan());

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array is not { } array)
        {
            return 0;
        }

        HashCode hc = default;
        foreach (var item in array)
        {
            hc.Add(item);
        }

        return hc.ToHashCode();
    }

    public ImmutableArray<T>.Enumerator GetEnumerator() => AsImmutableArray().GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)AsImmutableArray()).GetEnumerator();

    public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);

    public static implicit operator ImmutableArray<T>(EquatableArray<T> array) => array.AsImmutableArray();

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}

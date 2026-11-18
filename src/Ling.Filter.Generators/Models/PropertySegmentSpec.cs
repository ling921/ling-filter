namespace Ling.Filter.Generators.Models;

/// <summary>
/// Value-equatable description of a single property hop along an entity navigation path.
/// </summary>
/// <param name="PropertyName">C# property name.</param>
/// <param name="FullyQualifiedTypeName">Fully qualified CLR type name (<c>global::...</c>).</param>
/// <param name="UnwrappedTypeName">Fully qualified unwrapped scalar type name.</param>
/// <param name="IsReferenceType">Whether the property type is a reference type.</param>
/// <param name="IsNullableValueType">Whether the property type is <c>Nullable&lt;T&gt;</c>.</param>
internal sealed record PropertySegmentSpec(
    string PropertyName,
    string FullyQualifiedTypeName,
    string UnwrappedTypeName,
    bool IsReferenceType,
    bool IsNullableValueType);

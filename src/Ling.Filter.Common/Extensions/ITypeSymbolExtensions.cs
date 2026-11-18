using Ling.Filter.Helpers;
using Microsoft.CodeAnalysis;

namespace Ling.Filter.Extensions;

/// <summary>
/// Extension methods for <see cref="ITypeSymbol"/> classification and compatibility checks.
/// </summary>
internal static class ITypeSymbolExtensions
{
    /// <summary>
    /// Unwraps <see cref="Nullable{T}"/> if present, returning the underlying value type.
    /// </summary>
    public static ITypeSymbol UnwrapNullableValueType(this ITypeSymbol typeSymbol, out bool isNullableValueType)
    {
        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: 1 } nullable)
        {
            isNullableValueType = true;
            return nullable.TypeArguments[0];
        }

        isNullableValueType = false;
        return typeSymbol;
    }

    /// <summary>
    /// Determines whether a type is <see cref="string"/>.
    /// </summary>
    public static bool IsStringType(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.SpecialType == SpecialType.System_String;
    }

    /// <summary>
    /// Determines whether a type (after unwrapping <see cref="Nullable{T}"/>) is <see cref="bool"/>.
    /// </summary>
    public static bool IsBooleanType(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.UnwrapNullableValueType(out _).SpecialType == SpecialType.System_Boolean;
    }

    /// <summary>
    /// Determines whether a type (after unwrapping <see cref="Nullable{T}"/>) is an enum type.
    /// </summary>
    public static bool IsEnumType(this ITypeSymbol typeSymbol, out INamedTypeSymbol? enumType)
    {
        var unwrapped = typeSymbol.UnwrapNullableValueType(out _);
        if (unwrapped is INamedTypeSymbol { TypeKind: TypeKind.Enum } namedEnum)
        {
            enumType = namedEnum;
            return true;
        }

        enumType = null;
        return false;
    }

    /// <summary>
    /// Determines whether a type can hold <see langword="null"/> at runtime (reference type or <see cref="Nullable{T}"/>).
    /// </summary>
    public static bool CanBeNull(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.IsReferenceType ||
               typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
    }

    /// <summary>
    /// Determines whether <paramref name="typeSymbol"/> (unwrapped) is a recognized date/time type.
    /// </summary>
    public static bool IsDateOrTimeType(this ITypeSymbol typeSymbol, out DateTypeKind kind)
    {
        var unwrapped = typeSymbol.UnwrapNullableValueType(out _);
        if (unwrapped.SpecialType == SpecialType.System_DateTime)
        {
            kind = DateTypeKind.DateTime;
            return true;
        }

        var metadataName = unwrapped.GetFullyQualifiedMetadataName();
        kind = metadataName switch
        {
            "System.DateTimeOffset" => DateTypeKind.DateTimeOffset,
            "System.DateOnly" => DateTypeKind.DateOnly,
            "System.TimeOnly" => DateTypeKind.TimeOnly,
            _ => DateTypeKind.None,
        };

        return kind != DateTypeKind.None;
    }

    /// <summary>
    /// Determines whether <paramref name="typeSymbol"/> (unwrapped) supports relational operators (<c>&gt;</c>, <c>&gt;=</c>, <c>&lt;</c>, <c>&lt;=</c>) in C# LINQ expressions.
    /// </summary>
    public static bool SupportsRelationalOperators(this ITypeSymbol typeSymbol)
    {
        var unwrapped = typeSymbol.UnwrapNullableValueType(out _);
        if (unwrapped.IsNumericOrCharType() || unwrapped.IsDateOrTimeType(out _))
        {
            return true;
        }

        if (unwrapped.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        var metadataName = unwrapped.GetFullyQualifiedMetadataName();
        if (metadataName is "System.TimeSpan")
        {
            return true;
        }

        // Check user-defined comparison operators op_GreaterThan / op_LessThan
        return unwrapped.GetMembers("op_GreaterThan").OfType<IMethodSymbol>().Any() &&
               unwrapped.GetMembers("op_LessThan").OfType<IMethodSymbol>().Any();
    }

    /// <summary>
    /// Determines whether <paramref name="typeSymbol"/> (unwrapped) is a primitive numeric or <see cref="decimal"/>/<see cref="char"/> type.
    /// </summary>
    public static bool IsNumericOrCharType(this ITypeSymbol typeSymbol)
    {
        var unwrapped = typeSymbol.UnwrapNullableValueType(out _);
        return unwrapped.SpecialType is
            SpecialType.System_Byte or
            SpecialType.System_SByte or
            SpecialType.System_Int16 or
            SpecialType.System_UInt16 or
            SpecialType.System_Int32 or
            SpecialType.System_UInt32 or
            SpecialType.System_Int64 or
            SpecialType.System_UInt64 or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Decimal or
            SpecialType.System_Char;
    }

    /// <summary>
    /// Unwraps <c>ValueRange&lt;T&gt;</c> or <c>ValueRange&lt;T&gt;?</c> and extracts <c>T</c>.
    /// </summary>
    public static bool TryGetValueRangeElementType(this ITypeSymbol typeSymbol, out ITypeSymbol? elementType)
    {
        var unwrapped = typeSymbol.UnwrapNullableValueType(out _);
        if (unwrapped is INamedTypeSymbol { TypeArguments.Length: 1 } named &&
            named.GetFullyQualifiedMetadataName() == Constants.ValueRangeMetadataName)
        {
            elementType = named.TypeArguments[0];
            return true;
        }

        elementType = null;
        return false;
    }

    /// <summary>
    /// Extracts the element type <c>T</c> if <paramref name="typeSymbol"/> is an array or implements <c>IEnumerable&lt;T&gt;</c> (excluding <see cref="string"/>).
    /// </summary>
    public static bool TryGetCollectionElementType(this ITypeSymbol typeSymbol, out ITypeSymbol? elementType)
    {
        if (typeSymbol.IsStringType())
        {
            elementType = null;
            return false;
        }

        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }

        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T, TypeArguments.Length: 1 } directEnumerable)
        {
            elementType = directEnumerable.TypeArguments[0];
            return true;
        }

        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T &&
                iface.TypeArguments.Length == 1)
            {
                elementType = iface.TypeArguments[0];
                return true;
            }
        }

        elementType = null;
        return false;
    }

    /// <summary>
    /// Enumerates all constructed generic interfaces matching <paramref name="openGenericMetadataName"/> implemented by <paramref name="typeSymbol"/>.
    /// </summary>
    public static IEnumerable<INamedTypeSymbol> GetImplementedGenericInterfaces(
        this INamedTypeSymbol typeSymbol,
        string openGenericMetadataName)
    {
        if (typeSymbol.TypeKind == TypeKind.Interface &&
            typeSymbol.GetFullyQualifiedMetadataName() == openGenericMetadataName)
        {
            yield return typeSymbol;
        }

        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (iface.GetFullyQualifiedMetadataName() == openGenericMetadataName)
            {
                yield return iface;
            }
        }
    }

    /// <summary>
    /// Checks whether two scalar types (after unwrapping <see cref="Nullable{T}"/>) can be compared in C# expressions.
    /// </summary>
    public static bool IsScalarCompatibleWith(this ITypeSymbol filterType, ITypeSymbol targetType)
    {
        var left = filterType.UnwrapNullableValueType(out _);
        var right = targetType.UnwrapNullableValueType(out _);

        if (SymbolEqualityComparer.Default.Equals(left, right))
        {
            return true;
        }

        // Allow implicit numeric widening (e.g. int filter on long/decimal target)
        if (left.IsNumericOrCharType() && right.IsNumericOrCharType())
        {
            return true;
        }

        return false;
    }
}

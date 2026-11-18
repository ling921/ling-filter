using Microsoft.CodeAnalysis;

namespace Ling.Filter.Extensions;

/// <summary>
/// Extension methods for reading <see cref="AttributeData"/> constructor and named arguments safely.
/// </summary>
internal static class AttributeDataExtensions
{
    /// <summary>
    /// Reads a named argument of struct/primitive type <typeparamref name="T"/>, or returns <paramref name="defaultValue"/> if unspecified.
    /// </summary>
    public static T GetNamedValue<T>(this AttributeData attribute, string name, T defaultValue = default)
        where T : struct
    {
        foreach (var pair in attribute.NamedArguments)
        {
            if (pair.Key == name && pair.Value.Value is T value)
            {
                return value;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Reads an enum argument (from an underlying integer value) by named argument, falling back to a constructor argument at <paramref name="ctorIndex"/> if available.
    /// </summary>
    public static int GetEnumIntValue(
        this AttributeData attribute,
        string namedKey,
        int defaultValue,
        int ctorIndex = -1)
    {
        foreach (var pair in attribute.NamedArguments)
        {
            if (pair.Key == namedKey && pair.Value.Value is not null)
            {
                return Convert.ToInt32(pair.Value.Value);
            }
        }

        if (ctorIndex >= 0 &&
            ctorIndex < attribute.ConstructorArguments.Length &&
            attribute.ConstructorArguments[ctorIndex].Kind == TypedConstantKind.Enum &&
            attribute.ConstructorArguments[ctorIndex].Value is not null)
        {
            return Convert.ToInt32(attribute.ConstructorArguments[ctorIndex].Value);
        }

        return defaultValue;
    }

    /// <summary>
    /// Reads a string argument by named argument, falling back to a string constructor argument if present.
    /// </summary>
    public static string? GetStringValue(
        this AttributeData attribute,
        string namedKey,
        int fallbackCtorIndex = -1)
    {
        foreach (var pair in attribute.NamedArguments)
        {
            if (pair.Key == namedKey && pair.Value.Value is string namedStr)
            {
                return namedStr;
            }
        }

        if (fallbackCtorIndex >= 0 &&
            fallbackCtorIndex < attribute.ConstructorArguments.Length &&
            attribute.ConstructorArguments[fallbackCtorIndex].Value is string ctorStr)
        {
            return ctorStr;
        }

        return null;
    }

    /// <summary>
    /// Reads the <c>Target</c> string from a <c>FilterBaseAttribute</c>-derived attribute, checking both the named <c>Target</c> property
    /// and constructor string parameters.
    /// </summary>
    public static string? GetTargetPropertyPath(this AttributeData attribute)
    {
        foreach (var pair in attribute.NamedArguments)
        {
            if (pair.Key == "Target" && pair.Value.Value is string target && !string.IsNullOrWhiteSpace(target))
            {
                return target;
            }
        }

        // Check string constructor arguments: for (string target) it is index 0; for (Enum, string target) it is index 1.
        for (var i = attribute.ConstructorArguments.Length - 1; i >= 0; i--)
        {
            var arg = attribute.ConstructorArguments[i];
            if (arg.Kind == TypedConstantKind.Primitive &&
                arg.Type?.SpecialType == SpecialType.System_String &&
                arg.Value is string ctorStr &&
                !string.IsNullOrWhiteSpace(ctorStr))
            {
                return ctorStr;
            }
        }

        return null;
    }
}

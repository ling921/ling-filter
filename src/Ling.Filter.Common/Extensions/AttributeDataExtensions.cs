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

    /// <summary>
    /// Extracts <c>NavigationProperty</c>, <c>Quantifier</c>, and <c>ElementProperty</c> from a <c>CollectionFilterAttribute</c>,
    /// supporting all constructor overloads and named arguments.
    /// </summary>
    public static void GetCollectionFilterArgs(
        this AttributeData attribute,
        out string? navigationProperty,
        out int quantifier,
        out string? elementProperty)
    {
        navigationProperty = attribute.GetStringValue("NavigationProperty", fallbackCtorIndex: 0);
        quantifier = 0; // CollectionQuantifier.Any
        elementProperty = null;

        // Check constructor arguments beyond index 0:
        // Overload 2: (string navigationProperty, string elementProperty)
        // Overload 3: (string navigationProperty, CollectionQuantifier quantifier, string? elementProperty = null)
        if (attribute.ConstructorArguments.Length >= 2)
        {
            var arg1 = attribute.ConstructorArguments[1];
            if (arg1.Kind == TypedConstantKind.Enum && arg1.Value is not null)
            {
                quantifier = Convert.ToInt32(arg1.Value);
                if (attribute.ConstructorArguments.Length >= 3 &&
                    attribute.ConstructorArguments[2].Value is string elemFromArg2)
                {
                    elementProperty = elemFromArg2;
                }
            }
            else if (arg1.Value is string elemFromArg1)
            {
                elementProperty = elemFromArg1;
            }
        }

        // Named arguments override constructor arguments
        foreach (var pair in attribute.NamedArguments)
        {
            if (pair.Key == "Quantifier" && pair.Value.Value is not null)
            {
                quantifier = Convert.ToInt32(pair.Value.Value);
            }
            else if (pair.Key == "ElementProperty" && pair.Value.Value is string namedElem)
            {
                elementProperty = namedElem;
            }
        }
    }
}

using Ling.Filter.Analyzers.Diagnostics;
using Ling.Filter.Analyzers.Test.Common;

namespace Ling.Filter.Analyzers.Test;

public sealed class FilterPropertyAttributeAnalyzerTests
{
    [Fact]
    public async Task ReportsLF0002_WhenTargetPropertyPathDoesNotExist()
    {
        const string source = """
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class Dept { public string Name { get; set; } = ""; }
            public class User { public Dept? Department { get; set; } }

            public partial class UserFilter : IFilterable<User>
            {
                [{|#0:StringFilter(StringFilterMode.Contains, Target = "Department.Unknown")|}]
                public string? DeptKeyword { get; set; }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TargetPropertyPathNotFound)
            .WithLocation(0)
            .WithArguments("Department.Unknown", "User", "Unknown");

        await CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReportsLF0003_WhenAttributesAppliedToIncompatibleDtoTypes()
    {
        const string source = """
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class User
            {
                public int Age { get; set; }
                public string? Bio { get; set; }
            }

            public partial class UserFilter : IFilterable<User>
            {
                [{|#0:StringFilter|}]
                public int Age { get; set; }

                [{|#1:NullCheckFilter("Bio")|}]
                public string? HasBio { get; set; }

                [{|#2:InFilter("Age")|}]
                public string? Ages { get; set; }

                [{|#3:DateFilter|}]
                public int CreatedAt { get; set; }
            }
            """;

        var d0 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AttributeIncompatibleWithPropertyType)
            .WithLocation(0)
            .WithArguments("StringFilterAttribute", "Age", "int", "expected 'string'");

        var d1 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AttributeIncompatibleWithPropertyType)
            .WithLocation(1)
            .WithArguments("NullCheckFilterAttribute", "HasBio", "string?", "expected 'bool' or 'bool?'");

        var d2 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AttributeIncompatibleWithPropertyType)
            .WithLocation(2)
            .WithArguments("InFilterAttribute", "Ages", "string?", "expected an array or 'IEnumerable<T>' collection");

        var d3 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AttributeIncompatibleWithPropertyType)
            .WithLocation(3)
            .WithArguments("DateFilterAttribute", "CreatedAt", "int", "expected 'DateTime', 'DateTimeOffset', 'DateOnly', or 'TimeOnly'");

        await CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>.VerifyAnalyzerAsync(source, d0, d1, d2, d3);
    }

    [Fact]
    public async Task ReportsLF0004_WhenNullCheckTargetsNonNullableValueType()
    {
        const string source = """
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class User { public int Age { get; set; } }

            public partial class UserFilter : IFilterable<User>
            {
                [{|#0:NullCheckFilter("Age")|}]
                public bool? HasAge { get; set; }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.FilterAndTargetTypesIncompatible)
            .WithLocation(0)
            .WithArguments("HasAge", "bool?", "Age", "int", "NullCheckFilter target must be a reference type or Nullable<T>");

        await CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReportsLF0007_WhenCollectionFilterNavigationIsNotCollectionOrElementMissing()
    {
        const string source = """
            using System.Collections.Generic;
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class Tag { public string Code { get; set; } = ""; }
            public class Post
            {
                public string Title { get; set; } = "";
                public List<Tag> Tags { get; set; } = new();
            }

            public partial class PostFilter : IFilterable<Post>
            {
                [{|#0:CollectionFilter("Title", "Length")|}]
                public int? TitleLen { get; set; }

                [{|#1:CollectionFilter("Tags", "MissingProp")|}]
                public string? TagCode { get; set; }
            }
            """;

        var d0 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.InvalidCollectionFilterTarget)
            .WithLocation(0)
            .WithArguments("TitleLen", "navigation property 'Title' of type 'string' is not a collection");

        var d1 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.InvalidCollectionFilterTarget)
            .WithLocation(1)
            .WithArguments("TagCode", "element property path 'MissingProp' was not found on element type 'Tag' (missing 'MissingProp')");

        await CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>.VerifyAnalyzerAsync(source, d0, d1);
    }

    [Fact]
    public async Task ReportsLF0008_AndLF0009_ForConflictingAndEmptyAttributes()
    {
        const string source = """
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class User { public string Name { get; set; } = ""; }

            public partial class UserFilter : IFilterable<User>
            {
                [FilterIgnore]
                [StringFilter]
                public string? {|#0:Name|} { get; set; }

                [{|#1:MultiFieldFilter|}]
                public string? Keyword { get; set; }
            }
            """;

        var d0 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ConflictingFilterAttributes)
            .WithLocation(0)
            .WithArguments("Name", "FilterIgnoreAttribute, StringFilterAttribute");

        var d1 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.EmptyMultiFieldFilter)
            .WithLocation(1)
            .WithArguments("Keyword");

        await CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>.VerifyAnalyzerAsync(source, d0, d1);
    }

    [Fact]
    public async Task ReportsAdditionalLF0002_LF0003_LF0004_AndLF0008_ForRangeFlagsCompareAndMultiField()
    {
        const string source = """
            using System;
            using Ling.Filter;
            using Ling.Filter.Annotations;

            [Flags] public enum RoleA { None = 0, Read = 1 }
            [Flags] public enum RoleB { None = 0, Write = 1 }

            public class User
            {
                public Guid ExternalId { get; set; }
                public int Score { get; set; }
                public RoleA Role { get; set; }
            }

            public partial class UserFilter : IFilterable<User>
            {
                [{|#0:RangeFilter("Score")|}]
                public int NotARange { get; set; }

                [{|#1:FlagsFilter|}]
                public int NotAnEnum { get; set; }

                [{|#2:FlagsFilter(FlagsMatchMode.HasAll, "Role")|}]
                public RoleB? WrongEnum { get; set; }

                [{|#3:CompareFilter(CompareOperator.GreaterThan, "ExternalId")|}]
                public object? NonComparable { get; set; }

                [StringFilter]
                [CompareFilter]
                public string? {|#4:DoublePrimary|} { get; set; }

                [{|#5:MultiFieldFilter("MissingField", "Score")|}]
                public string? BadMulti { get; set; }
            }
            """;

        var d0 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AttributeIncompatibleWithPropertyType)
            .WithLocation(0)
            .WithArguments("RangeFilterAttribute", "NotARange", "int", "expected 'ValueRange<T>' where T is a comparable value type");

        var d1 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AttributeIncompatibleWithPropertyType)
            .WithLocation(1)
            .WithArguments("FlagsFilterAttribute", "NotAnEnum", "int", "expected an enum or nullable enum type");

        var d2 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.FilterAndTargetTypesIncompatible)
            .WithLocation(2)
            .WithArguments("WrongEnum", "RoleB?", "Role", "RoleA", "FlagsFilter property and target must share the same enum type");

        var d3 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AttributeIncompatibleWithPropertyType)
            .WithLocation(3)
            .WithArguments("CompareFilterAttribute", "NonComparable", "object?", "relational operators (>, >=, <, <=) require a numeric, date, enum, or comparable type");

        var d4 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ConflictingFilterAttributes)
            .WithLocation(4)
            .WithArguments("DoublePrimary", "StringFilterAttribute, CompareFilterAttribute");

        var d5Missing = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TargetPropertyPathNotFound)
            .WithLocation(5)
            .WithArguments("MissingField", "User", "MissingField");

        var d5Type = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.FilterAndTargetTypesIncompatible)
            .WithLocation(5)
            .WithArguments("BadMulti", "string", "Score", "int", "string pattern matching requires a string target field");

        await CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .VerifyAnalyzerAsync(source, d0, d1, d2, d3, d4, d5Missing, d5Type);
    }

    [Fact]
    public async Task ReportsLF0003_WhenRangeFilterDatePrecisionUsedOnNonDateValueRange_AndLF0007_ForThreeArgCollectionFilter()
    {
        const string source = """
            using System.Collections.Generic;
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class Tag { public string Code { get; set; } = ""; }
            public class Order
            {
                public int Amount { get; set; }
                public List<Tag> Tags { get; set; } = new();
            }

            public partial class OrderFilter : IFilterable<Order>
            {
                [{|#0:RangeFilter("Amount", Precision = DatePrecision.Day)|}]
                public ValueRange<int>? AmountRange { get; set; }

                [{|#1:CollectionFilter("Tags", CollectionQuantifier.Any, "MissingCode")|}]
                public string? TagCode { get; set; }
            }
            """;

        var d0 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AttributeIncompatibleWithPropertyType)
            .WithLocation(0)
            .WithArguments("RangeFilterAttribute", "AmountRange", "ValueRange<int>?", "DatePrecision requires 'ValueRange<T>' where T is 'DateTime', 'DateTimeOffset', 'DateOnly', or 'TimeOnly'");

        var d1 = CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.InvalidCollectionFilterTarget)
            .WithLocation(1)
            .WithArguments("TagCode", "element property path 'MissingCode' was not found on element type 'Tag' (missing 'MissingCode')");

        await CSharpAnalyzerVerifier<FilterPropertyAttributeAnalyzer>.VerifyAnalyzerAsync(source, d0, d1);
    }
}

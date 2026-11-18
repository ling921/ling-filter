using Ling.Filter.Analyzers.Diagnostics;
using Ling.Filter.Analyzers.Test.Common;

namespace Ling.Filter.Analyzers.Test;

public sealed class FilterTypeDeclarationAnalyzerTests
{
    [Fact]
    public async Task ReportsLF0001_WhenFilterTypeIsNotPartial()
    {
        const string source = """
            using Ling.Filter;

            public class User { public int Id { get; set; } }

            public class {|#0:UserFilter|} : IFilterable<User>
            {
                public int? Id { get; set; }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.FilterTypeMustBePartial)
            .WithLocation(0)
            .WithArguments("type 'UserFilter' must be declared partial");

        await CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReportsLF0001_WhenContainingTypeIsNotPartialOrGeneric()
    {
        const string source = """
            using Ling.Filter;

            public class User { public int Id { get; set; } }

            public class Outer
            {
                public partial class {|#0:UserFilter|} : IFilterable<User>
                {
                    public int? Id { get; set; }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.FilterTypeMustBePartial)
            .WithLocation(0)
            .WithArguments("containing type 'Outer' must be declared partial");

        await CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReportsLF0005_WhenConventionPropertyCannotMapToEntity()
    {
        const string source = """
            using Ling.Filter;

            public class User { public string Name { get; set; } = ""; }

            public partial class UserFilter : IFilterable<User>
            {
                public string? Name { get; set; }
                public string? {|#0:NonExistentField|} { get; set; }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.UnmappedFilterProperty)
            .WithLocation(0)
            .WithArguments("NonExistentField", "User");

        await CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DoesNotReportLF0005_ForPagedSortableOrIgnoredOrDisabledWarning()
    {
        const string source = """
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class User { public int Age { get; set; } }

            [FilterOptions(WarnOnUnmappedProperties = false)]
            public partial class SuppressedFilter : IFilterable<User>
            {
                public string? ExtraUiToken { get; set; }
            }

            public partial class PagedSortedFilter : IFilterable<User>, ISortable<User>, IPagedFilter
            {
                public int? MinAge { get; set; }
                [FilterIgnore]
                public string? TraceId { get; set; }
                public string? SortBy { get; set; }
                public bool SortDescending { get; set; }
                public int? Page { get; set; }
                public int? PageSize { get; set; }
            }
            """;

        await CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ReportsLF0002_WhenDefaultSortTargetsMissingField()
    {
        const string source = """
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class User { public int Id { get; set; } }

            [{|#0:DefaultSort("MissingSortProp")|}]
            public partial class UserSort : ISortable<User>
            {
                public string? SortBy { get; set; }
                public bool SortDescending { get; set; }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TargetPropertyPathNotFound)
            .WithLocation(0)
            .WithArguments("MissingSortProp", "User", "MissingSortProp");

        await CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReportsLF0004_WhenAutoMappedPropertyHasIncompatibleType()
    {
        const string source = """
            using System;
            using Ling.Filter;

            public class User { public int Age { get; set; } }

            public partial class UserFilter : IFilterable<User>
            {
                public Guid? {|#0:Age|} { get; set; }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.FilterAndTargetTypesIncompatible)
            .WithLocation(0)
            .WithArguments("Age", "Guid?", "Age", "int", "types are not comparable");

        await CSharpAnalyzerVerifier<FilterTypeDeclarationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}

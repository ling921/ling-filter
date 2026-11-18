using Ling.Filter.Analyzers.Diagnostics;
using Ling.Filter.Analyzers.Test.Common;
using Ling.Filter.CodeFixes;

namespace Ling.Filter.Analyzers.Test.CodeFixesTests;

public sealed class AddFilterIgnoreAttributeCodeFixTests
{
    [Fact]
    public async Task AddsFilterIgnoreAttribute_AndAnnotationsUsingDirective()
    {
        const string source = """
            using Ling.Filter;

            public class User { public int Id { get; set; } }

            public partial class UserFilter : IFilterable<User>
            {
                public int? Id { get; set; }
                public string? {|#0:CorrelationId|} { get; set; }
            }
            """;

        const string fixedSource = """
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class User { public int Id { get; set; } }

            public partial class UserFilter : IFilterable<User>
            {
                public int? Id { get; set; }
                [FilterIgnore]
                public string? CorrelationId { get; set; }
            }
            """;

        var expected = CSharpCodeFixVerifier<FilterTypeDeclarationAnalyzer, AddFilterIgnoreAttributeCodeFixProvider>
            .Diagnostic(DiagnosticDescriptors.UnmappedFilterProperty)
            .WithLocation(0)
            .WithArguments("CorrelationId", "User");

        await CSharpCodeFixVerifier<FilterTypeDeclarationAnalyzer, AddFilterIgnoreAttributeCodeFixProvider>
            .VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task PreservesExistingUsingDirective_WhenAlreadyImported()
    {
        const string source = """
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class User { public int Id { get; set; } }

            public partial class UserFilter : IFilterable<User>
            {
                public string? {|#0:CorrelationId|} { get; set; }
            }
            """;

        const string fixedSource = """
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class User { public int Id { get; set; } }

            public partial class UserFilter : IFilterable<User>
            {
                [FilterIgnore]
                public string? CorrelationId { get; set; }
            }
            """;

        var expected = CSharpCodeFixVerifier<FilterTypeDeclarationAnalyzer, AddFilterIgnoreAttributeCodeFixProvider>
            .Diagnostic(DiagnosticDescriptors.UnmappedFilterProperty)
            .WithLocation(0)
            .WithArguments("CorrelationId", "User");

        await CSharpCodeFixVerifier<FilterTypeDeclarationAnalyzer, AddFilterIgnoreAttributeCodeFixProvider>
            .VerifyCodeFixAsync(source, expected, fixedSource);
    }
}

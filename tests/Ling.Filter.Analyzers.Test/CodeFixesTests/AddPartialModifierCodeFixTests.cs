using Ling.Filter.Analyzers.Diagnostics;
using Ling.Filter.Analyzers.Test.Common;
using Ling.Filter.CodeFixes;

namespace Ling.Filter.Analyzers.Test.CodeFixesTests;

public sealed class AddPartialModifierCodeFixTests
{
    [Fact]
    public async Task AddsPartialToNestedAndContainingTypes()
    {
        const string source = """
            using Ling.Filter;

            public class User { public int Id { get; set; } }

            public class Outer
            {
                public class {|#0:UserFilter|} : IFilterable<User>
                {
                    public int? Id { get; set; }
                }
            }
            """;

        const string fixedSource = """
            using Ling.Filter;

            public class User { public int Id { get; set; } }

            public partial class Outer
            {
                public partial class UserFilter : IFilterable<User>
                {
                    public int? Id { get; set; }
                }
            }
            """;

        var expected = CSharpCodeFixVerifier<FilterTypeDeclarationAnalyzer, AddPartialModifierCodeFixProvider>
            .Diagnostic(DiagnosticDescriptors.FilterTypeMustBePartial)
            .WithLocation(0)
            .WithArguments("type 'UserFilter' must be declared partial");

        await CSharpCodeFixVerifier<FilterTypeDeclarationAnalyzer, AddPartialModifierCodeFixProvider>
            .VerifyCodeFixAsync(source, expected, fixedSource);
    }
}

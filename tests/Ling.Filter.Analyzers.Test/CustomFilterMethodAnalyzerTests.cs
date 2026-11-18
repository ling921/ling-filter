using Ling.Filter.Analyzers.Diagnostics;
using Ling.Filter.Analyzers.Test.Common;

namespace Ling.Filter.Analyzers.Test;

public sealed class CustomFilterMethodAnalyzerTests
{
    [Fact]
    public async Task ReportsLF0006_ForStaticParameterizedOrWrongReturnTypeMethods()
    {
        const string source = """
            using System;
            using System.Linq.Expressions;
            using Ling.Filter;
            using Ling.Filter.Annotations;

            public class Order { public int Id { get; set; } }

            public partial class OrderFilter : IFilterable<Order>
            {
                [CustomFilter]
                public static Expression<Func<Order, bool>>? {|#0:StaticRule|}() => null;

                [CustomFilter]
                public Expression<Func<Order, bool>>? {|#1:WithParam|}(int x) => null;

                [CustomFilter]
                public Func<Order, bool>? {|#2:WrongReturn|}() => null;

                [CustomFilter]
                public Expression<Func<Order, bool>>? ValidRule() => o => o.Id > 0;
            }
            """;

        var d0 = CSharpAnalyzerVerifier<CustomFilterMethodAnalyzer>
            .Diagnostic(DiagnosticDescriptors.InvalidCustomFilterMethod)
            .WithLocation(0)
            .WithArguments("StaticRule", "cannot be static");

        var d1 = CSharpAnalyzerVerifier<CustomFilterMethodAnalyzer>
            .Diagnostic(DiagnosticDescriptors.InvalidCustomFilterMethod)
            .WithLocation(1)
            .WithArguments("WithParam", "must be non-generic and take zero parameters");

        var d2 = CSharpAnalyzerVerifier<CustomFilterMethodAnalyzer>
            .Diagnostic(DiagnosticDescriptors.InvalidCustomFilterMethod)
            .WithLocation(2)
            .WithArguments("WrongReturn", "must return Expression<Func<Order, bool>>?");

        await CSharpAnalyzerVerifier<CustomFilterMethodAnalyzer>.VerifyAnalyzerAsync(source, d0, d1, d2);
    }
}

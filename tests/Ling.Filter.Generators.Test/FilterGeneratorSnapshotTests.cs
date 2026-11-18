using Ling.Filter.Generators.Test.Common;
using Microsoft.CodeAnalysis;

namespace Ling.Filter.Generators.Test;

public sealed class FilterGeneratorSnapshotTests
{
    [Fact]
    public void GeneratesCleanCode_ForStandardClassNestedRecordAndStruct()
    {
        const string source = """
            using System;
            using Ling.Filter;
            using Ling.Filter.Annotations;

            namespace DemoApp;

            public sealed class User
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public int Age { get; set; }
                public DateTime CreatedAt { get; set; }
            }

            public partial class OuterContainer
            {
                public sealed partial record NestedUserFilter : IFilterable<User>, ISortable<User>, IPagedFilter
                {
                    [StringFilter(StringFilterMode.Contains, Trim = true)]
                    public string? Name { get; set; }

                    public int? MinAge { get; set; }

                    [DateFilter(DatePrecision.Day)]
                    public DateTime? CreatedAt { get; set; }

                    public string? SortBy { get; set; }
                    public bool SortDescending { get; set; }
                    public int? Page { get; set; }
                    public int? PageSize { get; set; }
                }
            }

            public partial struct StructUserFilter : IFilterable<User>
            {
                public int? Id { get; set; }
            }
            """;

        var (outputCompilation, genDiagnostics, runResult) = GeneratorTestRunner.RunGenerator(source);

        Assert.Empty(genDiagnostics);
        var compileDiagnostics = outputCompilation.GetDiagnostics()
            .Where(static d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .ToArray();
        Assert.Empty(compileDiagnostics);

        Assert.Equal(2, runResult.GeneratedTrees.Length);

        var nestedGenerated = runResult.GeneratedTrees
            .Single(static t => t.FilePath.EndsWith("DemoApp.OuterContainer.NestedUserFilter.Filter.g.cs", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.Contains("partial class OuterContainer", nestedGenerated);
        Assert.Contains("partial record NestedUserFilter", nestedGenerated);
        Assert.Contains("public global::System.Linq.Expressions.Expression<global::System.Func<global::DemoApp.User, bool>> ToExpression()", nestedGenerated);
        Assert.Contains("public global::System.Linq.IQueryable<global::DemoApp.User> Apply(", nestedGenerated);
        Assert.Contains("public global::System.Linq.IQueryable<global::DemoApp.User> ApplySort(", nestedGenerated);
        Assert.Contains("DatePrecisionHelper.GetRange", nestedGenerated);
    }

    [Fact]
    public void GeneratesExplicitInterfaceAndNamedMethods_WhenImplementingMultipleEntities()
    {
        const string source = """
            using Ling.Filter;

            namespace DemoApp;

            public sealed class ActiveOrder { public string Code { get; set; } = ""; }
            public sealed class ArchivedOrder { public string Code { get; set; } = ""; }

            public sealed partial class SharedOrderFilter : IFilterable<ActiveOrder>, IFilterable<ArchivedOrder>
            {
                public string? Code { get; set; }
            }
            """;

        var (outputCompilation, genDiagnostics, runResult) = GeneratorTestRunner.RunGenerator(source);

        Assert.Empty(genDiagnostics);
        Assert.Empty(outputCompilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));

        var generated = Assert.Single(runResult.GeneratedTrees).GetText().ToString();
        Assert.Contains("IFilterable<global::DemoApp.ActiveOrder>.ToExpression()", generated);
        Assert.Contains("ToActiveOrderExpression()", generated);
        Assert.Contains("IFilterable<global::DemoApp.ArchivedOrder>.ToExpression()", generated);
        Assert.Contains("ToArchivedOrderExpression()", generated);
    }

    [Fact]
    public void RespectsUserDefinedToExpressionOrApplyOverrides()
    {
        const string source = """
            using System;
            using System.Linq.Expressions;
            using Ling.Filter;

            namespace DemoApp;

            public sealed class Item { public int Id { get; set; } }

            public sealed partial class CustomItemFilter : IFilterable<Item>
            {
                public int? Id { get; set; }

                public Expression<Func<Item, bool>> ToExpression() => x => x.Id > 0;
            }
            """;

        var (outputCompilation, genDiagnostics, runResult) = GeneratorTestRunner.RunGenerator(source);

        Assert.Empty(genDiagnostics);
        Assert.Empty(outputCompilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));

        var generated = Assert.Single(runResult.GeneratedTrees).GetText().ToString();
        Assert.DoesNotContain("ToExpression()", generated);
        Assert.Contains("Apply(", generated);
    }
}

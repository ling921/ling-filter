using Ling.Filter.Generators;
using Ling.Filter.Generators.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ling.Filter.Generators.Test;

public sealed class FilterGeneratorCachingTests
{
    [Fact]
    public void IncrementalPipeline_CachesOutput_WhenUnrelatedSourceChanges()
    {
        const string filterSource = """
            using Ling.Filter;
            using Ling.Filter.Annotations;

            namespace DemoApp;

            public sealed class Product
            {
                public int Id { get; set; }
                public string Title { get; set; } = "";
            }

            public sealed partial class ProductFilter : IFilterable<Product>
            {
                [StringFilter(StringFilterMode.Contains)]
                public string? Title { get; set; }
            }
            """;

        const string unrelatedV1 = """
            namespace DemoApp;
            public static class Helper { public static int Add(int a, int b) => a + b; }
            """;

        const string unrelatedV2 = """
            namespace DemoApp;
            public static class Helper { public static int Add(int a, int b) => a + b + 1; }
            """;

        var compilation1 = GeneratorTestRunner.CreateCompilation(filterSource, unrelatedV1);
        var generator = new FilterGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation1);
        var firstRun = driver.GetRunResult().Results.Single();
        Assert.Single(firstRun.GeneratedSources);

        // Replace only the unrelated syntax tree
        var updatedTree = CSharpSyntaxTree.ParseText(unrelatedV2, new CSharpParseOptions(LanguageVersion.Latest), path: "Source1.cs");
        var compilation2 = compilation1.ReplaceSyntaxTree(compilation1.SyntaxTrees[1], updatedTree);

        driver = driver.RunGenerators(compilation2);
        var secondRun = driver.GetRunResult().Results.Single();

        // All final output steps must be cached
        foreach (var (_, steps) in secondRun.TrackedOutputSteps)
        {
            foreach (var step in steps)
            {
                foreach (var (_, reason) in step.Outputs)
                {
                    Assert.Equal(IncrementalStepRunReason.Cached, reason);
                }
            }
        }
    }
}

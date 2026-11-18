using System.Collections.Immutable;
using Ling.Filter.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ling.Filter.Generators.Test.Common;

internal static class GeneratorTestRunner
{
    public static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult RunResult) RunGenerator(
        params string[] sources)
    {
        var compilation = CreateCompilation(sources);
        var generator = new FilterGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    public static CSharpCompilation CreateCompilation(params string[] sources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var trees = sources.Select((s, i) => CSharpSyntaxTree.ParseText(s, parseOptions, path: $"Source{i}.cs")).ToArray();

        var references = new List<MetadataReference>(Basic.Reference.Assemblies.Net80.References.All)
        {
            MetadataReference.CreateFromFile(typeof(IFilterable<>).Assembly.Location),
        };

        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);

        return CSharpCompilation.Create(
            assemblyName: "Ling.Filter.DynamicTestAssembly",
            syntaxTrees: trees,
            references: references,
            options: options);
    }
}

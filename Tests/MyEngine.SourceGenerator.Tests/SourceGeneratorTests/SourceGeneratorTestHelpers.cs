using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

public static class SourceGeneratorTestHelpers
{
    private static readonly string SystemDllsDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

    private static readonly IReadOnlyList<MetadataReference> SystemReferences = new[]
    {
        MetadataReference.CreateFromFile(Path.Combine(SystemDllsDirectory, "System.Runtime.dll")),
        MetadataReference.CreateFromFile(Path.Combine(SystemDllsDirectory, "System.Private.CoreLib.dll")),
    };

    public static Compilation CreateCompilation(string source,
        IEnumerable<KeyValuePair<string, string>> referenceSources,
        IEnumerable<Assembly> referenceAssemblies)
    {
        var referenceAssemblyReferences = referenceAssemblies.Select(x => MetadataReference.CreateFromFile(x.Location));

        var referenceCompilations = referenceSources.Select(x =>
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(x.Value);
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var compilation = CSharpCompilation.Create(
                assemblyName: x.Key,
                syntaxTrees: new[] { syntaxTree },
                references: SystemReferences.Concat(referenceAssemblyReferences),
                options: compilationOptions);

            var diagnostics = compilation.GetDiagnostics();

            diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);

            return compilation;
        });

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary);

        var compilation = CSharpCompilation.Create(
            assemblyName: "MyEngine.SourceGenerator.Tests",
            syntaxTrees: new[] { syntaxTree },
            references: SystemReferences
                .Concat(referenceCompilations.Select(x => x.ToMetadataReference() as MetadataReference))
                .Concat(referenceAssemblyReferences),
            compilationOptions);


        var diagnostics = compilation.GetDiagnostics();

        diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);

        return compilation;
    }

    private static GeneratorDriver GetGeneratorDriver(
        string source,
        IEnumerable<KeyValuePair<string, string>> referenceSources,
        IEnumerable<Assembly> referenceAssemblies,
        IIncrementalGenerator generator)
    {
        var compilation = CreateCompilation(source, referenceSources, referenceAssemblies);

        return CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
    }

    public static Task VerifyGeneratorOutput(string source,
        IEnumerable<KeyValuePair<string, string>> referenceSources,
        IEnumerable<Assembly> referenceAssemblies,
        IIncrementalGenerator generator,
        object[]? parameters = null)
    {
        var driver = GetGeneratorDriver(source, referenceSources, referenceAssemblies, generator);
        var runResult = driver.GetRunResult();
        runResult.Results.Should().ContainSingle()
            .Which.GeneratedSources.Should().ContainSingle();

        var settings = new VerifySettings();
        settings.UseDirectory("Expectations");
        if (parameters != null)
        {
            settings.UseParameters(parameters);
        }

        return Verify(driver, settings);
    }

    public static GeneratorDriverRunResult GetRunResult(string source,
        IEnumerable<KeyValuePair<string, string>> referenceSources,
        IEnumerable<Assembly> referenceAssemblies,
        IIncrementalGenerator generator)
    {
        var driver = GetGeneratorDriver(source, referenceSources, referenceAssemblies, generator);

        var runResult = driver.GetRunResult();

        runResult.Results.Should().AllSatisfy(x => x.Exception.Should().BeNull());

        return runResult;
    }
}

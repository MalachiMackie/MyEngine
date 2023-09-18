using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
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

    private static GeneratorDriver GetGeneratorDriver(string source, Assembly referenceAssembly, IIncrementalGenerator generator)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: SystemReferences.Append(MetadataReference.CreateFromFile(referenceAssembly.Location)),
            compilationOptions);

        var diagnostics = compilation.GetDiagnostics();

        diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);

        return CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
    }

    public static Task VerifyGeneratorOutput(string source, Assembly referenceAssembly, IIncrementalGenerator generator)
    {
        var driver = GetGeneratorDriver(source, referenceAssembly, generator);

        var settings = new VerifySettings();
        settings.UseDirectory("Expectations");

        return Verify(driver, settings);
    }

    public static GeneratorDriverRunResult GetRunResult(string source, Assembly referenceAssembly, IIncrementalGenerator generator)
    {
        var driver = GetGeneratorDriver(source, referenceAssembly, generator);
        return driver.GetRunResult();
    }
}

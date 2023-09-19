using FluentAssertions;
using MyEngine.Core;
using MyEngine.SourceGenerator.Generators;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

[UsesVerify]
public class AppEntrypointGeneratorTests
{
    [Fact]
    public async Task Should_GenerateAppEntrypointInfo()
    {
        var source = """
            using MyEngine.Core;

            namespace MyNamespace
            {
                [AppEntrypoint]
                public class MyAppEntrypoint : IAppEntrypoint
                {
                    public void BuildApp(AppBuilder builder)
                    {
                    }
                }
            }
            """;

        var generator = new AppEntrypointInfoSourceGenerator();

        await SourceGeneratorTestHelpers.VerifyGeneratorOutput(source,
            Array.Empty<KeyValuePair<string, string>>(),
            new[] { typeof(AppEntrypointAttribute).Assembly },
            generator);
    }

    [Fact]
    public void Should_NotGenerateAppEntrypointInfo_When_ClassIsNotIAppEntrypoint()
    {
        var source = """
            using MyEngine.Core;

            namespace MyNamespace
            {
                [AppEntrypoint]
                public class MyClass
                {
                    public void BuildApp(AppBuilder builder)
                    {
                    }
                }
            }
            """;

        var generator = new AppEntrypointInfoSourceGenerator();

        var runResult = SourceGeneratorTestHelpers.GetRunResult(source,
            Array.Empty<KeyValuePair<string, string>>(),
            new[] { typeof(AppEntrypointAttribute).Assembly },
            generator);
        runResult.Results.Should().ContainSingle()
            .Which.GeneratedSources.Should().BeEmpty();
    }
}

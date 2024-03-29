using MyEngine.Core;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

[UsesVerify]
public class EcsEngineSourceGeneratorTests
{

    private const string AppEntrypointSource = """
        using MyEngine.Core;

        [assembly: AppEntrypoint]

        [AppEntrypoint]
        public class MyAppEntrypoint : IAppEntrypoint
        {

            public void BuildApp(AppBuilder builder)
            {
            }
        }
        """;


    private const string SystemsReferenceSource = """
        using MyEngine.Core;
        using MyEngine.Core.Ecs.Resources;
        using MyEngine.Core.Ecs.Systems;

        namespace MyNamespace {
            public class MySystem : ISystem
            {
                public MySystem(MyResource resource)
                {
                }

                public void Run(double deltaTime)
                {
                }
            }

            public class MyResource : IResource
            {
            }

            public class MyStartupSystem : IStartupSystem
            {
                public MyStartupSystem(MyResource resource)
                {
                }

                public void Run()
                {
                }
            }

            
        }
        
        """;

    [Fact]
    public async Task Should_GenerateEcsEngine()
    {
        await SourceGeneratorTestHelpers.VerifyGeneratorOutput(AppEntrypointSource,
            new[]
            {
                KeyValuePair.Create("Systems", SystemsReferenceSource),
            },
            new[]
            {
                typeof(AppEntrypointAttribute).Assembly,
            },
            new EcsEngineSourceGenerator());
    }

    [Fact]
    public void Should_NotGenerateEcsEngine_When_AssemblyAppEntrypointAttributeIsNotSet()
    {
        var runResult = SourceGeneratorTestHelpers.GetRunResult("""
            using MyEngine.Core;

            // [assembly: AppEntrypoint]
            
            [AppEntrypoint]
            public class AppEntrypoint : IAppEntrypoint
            {
                public void BuildApp(AppBuilder builder)
                {
                }
            }
            """,
            new[]
            {
                KeyValuePair.Create("Systems", SystemsReferenceSource),
            },
            new[]
            {
                typeof(AppEntrypointAttribute).Assembly,
            },
            new EcsEngineSourceGenerator());

        runResult.Results.Should().ContainSingle()
            .Which.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void Should_NotGenerateEcsEngine_When_AppEntrypointHasNoAttribute()
    {
        var runResult = SourceGeneratorTestHelpers.GetRunResult("""
            using MyEngine.Core;

            [assembly: AppEntrypoint]
            
            // [AppEntrypoint]
            public class AppEntrypoint : IAppEntrypoint
            {
                public void BuildApp(AppBuilder builder)
                {
                }
            }
            """,
            new[]
            {
                KeyValuePair.Create("Systems", SystemsReferenceSource),
            },
            new[]
            {
                typeof(AppEntrypointAttribute).Assembly,
            },
            new EcsEngineSourceGenerator());

        runResult.Results.Should().ContainSingle()
            .Which.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void Should_NotGenerateEcsEngine_When_AppEntrypointDoesNotImplementIAppEntrypointInterface()
    {
        var runResult = SourceGeneratorTestHelpers.GetRunResult("""
            using MyEngine.Core;

            namespace MyNamespace;

            [AppEntrypoint]
            public class MyAppEntrypoint
            {
                public void BuildApp(AppBuilder builder)
                {
                }
            }
            """,
            new[]
            {
                KeyValuePair.Create("Systems", SystemsReferenceSource),
            },
            new[]
            {
                typeof(AppEntrypointAttribute).Assembly,
            },
            new EcsEngineSourceGenerator());

        runResult.Results.Should().ContainSingle()
            .Which.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_GenerateEcsEngine_When_NoAppSystemsInfoIsFound()
    {
        await SourceGeneratorTestHelpers.VerifyGeneratorOutput(AppEntrypointSource,
            Array.Empty<KeyValuePair<string, string>>(),
            new[]
            {
                typeof(AppEntrypointAttribute).Assembly,
            },
            new EcsEngineSourceGenerator());
    }
}

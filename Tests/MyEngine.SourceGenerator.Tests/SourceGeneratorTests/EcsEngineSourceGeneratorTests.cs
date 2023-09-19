using FluentAssertions;
using MyEngine.Core;
using MyEngine.Runtime;
using MyEngine.SourceGenerator.Generators;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

[UsesVerify]
public class EcsEngineSourceGeneratorTests
{
    private const string AppEntrypointInfoReferenceSource = """
        namespace MyEngine.Runtime
        {
            public class AppEntrypointInfo
            {
                public const string FullyQualifiedName = "MyAppEntrypoint";
            }
        }
        """;

    private const string AppSystemsInfoReferenceSource = """
        namespace Tests.Generated
        {
            [global::MyEngine.Core.AppSystemsInfo]
            public static class AppSystemsInfo
            {
                public const string SystemClasses = "[{\"FullyQualifiedName\":\"global::MyNamespace.MySystem\",\"Constructor\":{\"TotalParameters\":1,\"QueryParameters\":[],\"ResourceParameters\":[{\"Name\":\"MyNamespace.MyResource\",\"ParameterIndex\":0}]}}]";

                public const string StartupSystemClasses = "[{\"FullyQualifiedName\":\"global::MyNamespace.MyStartupSystem\",\"Constructor\":{\"Parameters\":[{\"Name\":\"MyNamespace.MyResource\"}]}}]";
            }
        }
        """;

    [Fact]
    public async Task Should_GenerateEcsEngine()
    {
        await SourceGeneratorTestHelpers.VerifyGeneratorOutput("[assembly: MyEngine.Runtime.EngineRuntimeAssembly]",
            new[]
            {
                KeyValuePair.Create("AppSystemsInfo", AppSystemsInfoReferenceSource),
                KeyValuePair.Create("AppEntrypointInfo", AppEntrypointInfoReferenceSource)
            },
            new[]
            {
                typeof(AppSystemsInfoAttribute).Assembly,
                typeof(EcsEngine).Assembly
            },
            new EcsEngineSourceGenerator());
    }

    [Fact]
    public void Should_NotGenerateEcsEngine_When_EngineRuntimeAssemblyAttributeIsNotSet()
    {
        var runResult = SourceGeneratorTestHelpers.GetRunResult("",
            new[]
            {
                KeyValuePair.Create("AppSystemsInfo", AppSystemsInfoReferenceSource),
                KeyValuePair.Create("AppEntrypointInfo", AppEntrypointInfoReferenceSource)
            },
            new[]
            {
                typeof(AppSystemsInfoAttribute).Assembly,
                typeof(EcsEngine).Assembly
            },
            new EcsEngineSourceGenerator());

        runResult.Results.Should().ContainSingle()
            .Which.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void Should_GenerateEcsEngine_When_NoAppEntrypointIsFound()
    {
        var runResult = SourceGeneratorTestHelpers.GetRunResult("[assembly: MyEngine.Runtime.EngineRuntimeAssembly]",
            new[]
            {
                KeyValuePair.Create("AppSystemsInfo", AppSystemsInfoReferenceSource),
            },
            new[]
            {
                typeof(AppSystemsInfoAttribute).Assembly,
                typeof(EcsEngine).Assembly
            },
            new EcsEngineSourceGenerator());

        runResult.Results.Should().ContainSingle()
            .Which.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_GenerateEcsEngine_When_NoAppSystemsInfoIsFound()
    {
        await SourceGeneratorTestHelpers.VerifyGeneratorOutput("[assembly: MyEngine.Runtime.EngineRuntimeAssembly]",
            new[]
            {
                KeyValuePair.Create("AppEntrypointInfo", AppEntrypointInfoReferenceSource)
            },
            new[]
            {
                typeof(AppSystemsInfoAttribute).Assembly,
                typeof(EcsEngine).Assembly
            },
            new EcsEngineSourceGenerator());
    }

    [Fact]
    public async Task Should_GenerateEmptyEcsEngine_When_AppSystemsInfoIsEmpty()
    {
        await SourceGeneratorTestHelpers.VerifyGeneratorOutput("[assembly: MyEngine.Runtime.EngineRuntimeAssembly]",
            new[]
            {
                KeyValuePair.Create("AppEntrypointInfo", AppEntrypointInfoReferenceSource),
                KeyValuePair.Create("AppSystemsInfo", """
                namespace Tests.Generated
                {
                    [global::MyEngine.Core.AppSystemsInfo]
                    public static class AppSystemsInfo
                    {
                        public const string SystemClasses = "[]";

                        public const string StartupSystemClasses = "[]";
                    }
                }
                """),
            },
            new[]
            {
                typeof(AppSystemsInfoAttribute).Assembly,
                typeof(EcsEngine).Assembly
            },
            new EcsEngineSourceGenerator());
    }
}

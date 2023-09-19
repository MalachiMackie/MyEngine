using FluentAssertions;
using MyEngine.Core;
using MyEngine.SourceGenerator.Generators;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

[UsesVerify]
public class EcsEngineSourceGeneratorTests
{
    private const string AppEntrypointInfoReferenceSource = """
        namespace MyEngine.Runtime
        {
            [global::MyEngine.Core.AppEntrypointInfo]
            public class AppEntrypointInfo
            {
                [global::MyEngine.Core.AppEntrypointInfoFullyQualifiedName]
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
                [global::MyEngine.Core.SystemClasses]
                public const string SystemClassesWithRandomName = "[{\"FullyQualifiedName\":\"global::MyNamespace.MySystem\",\"Constructor\":{\"TotalParameters\":1,\"QueryParameters\":[],\"ResourceParameters\":[{\"Name\":\"MyNamespace.MyResource\",\"ParameterIndex\":0}]}}]";

                [global::MyEngine.Core.StartupSystemClasses]
                public const string StartupSystemClassesWithRandomName = "[{\"FullyQualifiedName\":\"global::MyNamespace.MyStartupSystem\",\"Constructor\":{\"Parameters\":[{\"Name\":\"MyNamespace.MyResource\"}]}}]";
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
            },
            new EcsEngineSourceGenerator());
    }

    [Theory]
    [InlineData("IntField", "[global::MyEngine.Core.StartupSystemClasses] public const int StartupSystemClasses = 1;")]
    [InlineData("PrivateField", "[global::MyEngine.Core.StartupSystemClasses] private const string StartupSystemClasses = \"[{\\\"FullyQualifiedName\\\":\\\"global::MyNamespace.MyStartupSystem\\\",\\\"Constructor\\\":{\\\"Parameters\\\":[{\\\"Name\\\":\\\"MyNamespace.MyResource\\\"}]}}]\";")]
    [InlineData("MissingField", "")]
    [InlineData("StaticReadonlyField", "[global::MyEngine.Core.StartupSystemClasses] public static readonly string StartupSystemClasses = \"[{\\\"FullyQualifiedName\\\":\\\"global::MyNamespace.MyStartupSystem\\\",\\\"Constructor\\\":{\\\"Parameters\\\":[{\\\"Name\\\":\\\"MyNamespace.MyResource\\\"}]}}]\";")]
    public async Task Should_NotGenerateStartupSystems_When_StartupSystemClassesIsNotCorrectType(string description, string startupSystemClassesField)
    {
        await SourceGeneratorTestHelpers.VerifyGeneratorOutput("[assembly: MyEngine.Runtime.EngineRuntimeAssembly]",
            new[]
            {
                KeyValuePair.Create("AppEntrypointInfo", AppEntrypointInfoReferenceSource),
                KeyValuePair.Create("AppSystemsInfo", $$$"""
                    namespace Tests.Generated
                    {
                        [global::MyEngine.Core.AppSystemsInfo]
                        public static class AppSystemsInfo
                        {
                            [global::MyEngine.Core.SystemClasses]
                            public const string SystemClasses = "[{\"FullyQualifiedName\":\"global::MyNamespace.MySystem\",\"Constructor\":{\"TotalParameters\":1,\"QueryParameters\":[],\"ResourceParameters\":[{\"Name\":\"MyNamespace.MyResource\",\"ParameterIndex\":0}]}}]";

                            {{{startupSystemClassesField}}}
                        }
                    }
                """)
            },
            new[]
            {
                typeof(AppSystemsInfoAttribute).Assembly
            },
            new EcsEngineSourceGenerator(),
            new object[] { description });
    }

    [Theory]
    [InlineData("IntField", "[global::MyEngine.Core.SystemClasses] public const int SystemClasses = 1;")]
    [InlineData("PrivateField", "[global::MyEngine.Core.SystemClasses] private const string SystemClasses = \"[{\\\"FullyQualifiedName\\\":\\\"global::MyNamespace.MySystem\\\",\\\"Constructor\\\":{\\\"TotalParameters\\\":1,\\\"QueryParameters\\\":[],\\\"ResourceParameters\\\":[{\\\"Name\\\":\\\"MyNamespace.MyResource\\\",\\\"ParameterIndex\\\":0}]}}]\";")]
    [InlineData("MissingField", "")]
    [InlineData("StaticReadonlyField", "[global::MyEngine.Core.SystemClasses] public static readonly string SystemClasses = \"[{\\\"FullyQualifiedName\\\":\\\"global::MyNamespace.MySystem\\\",\\\"Constructor\\\":{\\\"TotalParameters\\\":1,\\\"QueryParameters\\\":[],\\\"ResourceParameters\\\":[{\\\"Name\\\":\\\"MyNamespace.MyResource\\\",\\\"ParameterIndex\\\":0}]}}]\";")]
    public async Task Should_NotGenerateSystems_When_SystemClassesIsNotCorrectType(string description, string systemClassesField)
    {
        await SourceGeneratorTestHelpers.VerifyGeneratorOutput("[assembly: MyEngine.Runtime.EngineRuntimeAssembly]",
            new[]
            {
                KeyValuePair.Create("AppEntrypointInfo", AppEntrypointInfoReferenceSource),
                KeyValuePair.Create("AppSystemsInfo", $$$"""
                    namespace Tests.Generated
                    {
                        [global::MyEngine.Core.AppSystemsInfo]
                        public static class AppSystemsInfo
                        {
                            {{{systemClassesField}}}

                            [global::MyEngine.Core.StartupSystemClasses]
                            public const string StartupSystemClasses = "[{\"FullyQualifiedName\":\"global::MyNamespace.MyStartupSystem\",\"Constructor\":{\"Parameters\":[{\"Name\":\"MyNamespace.MyResource\"}]}}]";
                        }
                    }
                """)
            },
            new[]
            {
                typeof(AppSystemsInfoAttribute).Assembly
            },
            new EcsEngineSourceGenerator(),
            new object[] { description });
    }
}

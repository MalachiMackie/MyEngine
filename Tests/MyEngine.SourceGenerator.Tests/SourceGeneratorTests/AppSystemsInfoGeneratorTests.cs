using MyEngine.Core.Ecs.Systems;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

[UsesVerify]
public class AppSystemsInfoGeneratorTests
{
    [Fact]
    public async Task Should_GenerateAppSystemsInfo()
    {
        var source = """
            using MyEngine.Core.Ecs.Systems;
            using MyEngine.Core.Ecs.Resources;

            namespace MyNamespace
            {
                public class MyResource : IResource {}

                public class MyStartupSystem : IStartupSystem
                {
                    public MyStartupSystem(MyResource resource)
                    {
                    }

                    public void Run()
                    {
                    }
                }

                public class MySystem : ISystem
                {
                    public MySystem(MyResource resource)
                    {
                    }

                    public void Run(double dt)
                    {
                    }
                }
            }
            """;

        await SourceGeneratorTestHelpers.VerifyGeneratorOutput(source,
            Array.Empty<KeyValuePair<string, string>>(),
            new[] { typeof(ISystem).Assembly },
            new AppSystemsInfoSourceGenerator());
    }
}

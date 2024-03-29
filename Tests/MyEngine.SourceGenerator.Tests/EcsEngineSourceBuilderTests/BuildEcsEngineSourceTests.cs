
namespace MyEngine.SourceGenerator.Tests.EcsEngineSourceBuilderTests;

[UsesVerify]
public class BuildEcsEngineSourceTests
{
    [Fact]
    public async Task Should_BuildFullEcsEngineSourceAsync()
    {
        var appEntrypointFullyQualifiedName = "MyAppEntrypoint";

        var systemClasses = new[]
        {
            new SystemClass("MySystem1", new SystemConstructor()
                .WithParameter(new SystemConstructorResourceParameter("Resource1"))
                .WithParameter(new SystemConstructorResourceParameter("Resource2"))
            ),
            new SystemClass("MySystem2", new SystemConstructor())
        };

        var startupSystemClasses = new[]
        {
            new StartupSystemClass(
                "MyStartupSystemClass1",
                new StartupSystemConstructor(
                    [
                        new StartupSystemConstructorParameter("Resource1"),
                        new StartupSystemConstructorParameter("Resource2"),
                    ])),
            new StartupSystemClass(
                "MyStartupSystemClass2",
                StartupSystemConstructor.Empty)
        };

        var (fileName, result) = EcsEngineGlueSourceBuilder.BuildEcsEngineGlueSource(
            startupSystemClasses,
            systemClasses,
            appEntrypointFullyQualifiedName);

        fileName.Should().Be("EcsEngineGlue.g.cs");

        await Verify(result);
    }
}

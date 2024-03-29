namespace MyEngine.SourceGenerator.Tests.EcsEngineSourceBuilderTests;

[UsesVerify]
public class SystemStartupInstantiationTests
{
    [Fact]
    public async Task Should_BuildFullStartupSystemInstantiation()
    {
        var startupSystemClass = new StartupSystemClass(
            "MyStartupSystem<string>",
            new StartupSystemConstructor(new[]
            {
                new StartupSystemConstructorParameter("Parameter1<bool>"),
                new StartupSystemConstructorParameter("Parameter2<int>"),
            }));

        var output = EcsEngineGlueSourceBuilder.BuildStartupSystemInstantiation(startupSystemClass);

        await Verify(output);
    }

    [Fact]
    public async Task Should_BuildStartupSystemInstantiation_When_ThereAreNoParameters()
    {
        var startupSystemClass = new StartupSystemClass("MyStartupSystem", StartupSystemConstructor.Empty);

        var output = EcsEngineGlueSourceBuilder.BuildStartupSystemInstantiation(startupSystemClass);

        await Verify(output);
    }
}

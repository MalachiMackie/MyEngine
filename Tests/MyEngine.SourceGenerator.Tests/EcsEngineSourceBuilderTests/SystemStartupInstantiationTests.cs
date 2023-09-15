using FluentAssertions;

namespace MyEngine.SourceGenerator.Tests.EcsEngineSourceBuilderTests;
public class SystemStartupInstantiationTests
{

    [Fact]
    public void Should_BuildFullStartupSystemInstantiation()
    {
        var startupSystemClass = new StartupSystemClassDto
        {
            FullyQualifiedName = "MyStartupSystem<string>",
            Constructor = new StartupSystemConstructorDto
            {
                Parameters = new[]
                {
                    new StartupSystemConstructorParameterDto()
                    {
                        Name = "Parameter1<bool>"
                    },
                    new StartupSystemConstructorParameterDto()
                    {
                        Name = "Parameter2<int>"
                    }
                }
            }
        };

        var output = EcsEngineSourceBuilder.BuildStartupSystemInstantiation(startupSystemClass);

        output.Should().Be(@"_startupSystemInstantiations.Add(typeof(MyStartupSystem<string>), () =>
{
    if (_resourceContainer.TryGetResource<Parameter1<bool>>(out var resource1)
        && _resourceContainer.TryGetResource<Parameter2<int>>(out var resource2))
    {
        return new MyStartupSystem<string>(resource1, resource2);
    }

    return null;
});
");
    }

    [Fact]
    public void Should_BuildStartupSystemInstantiation_When_ThereAreNoParameters()
    {
        var startupSystemClass = new StartupSystemClassDto
        {
            Constructor = new StartupSystemConstructorDto() { Parameters = Array.Empty<StartupSystemConstructorParameterDto>() },
            FullyQualifiedName = "MyStartupSystem"
        };

        var output = EcsEngineSourceBuilder.BuildStartupSystemInstantiation(startupSystemClass);

        output.Should().Be(@"_startupSystemInstantiations.Add(typeof(MyStartupSystem), () =>
{
    if (true)
    {
        return new MyStartupSystem();
    }

    return null;
});
");
    }
}

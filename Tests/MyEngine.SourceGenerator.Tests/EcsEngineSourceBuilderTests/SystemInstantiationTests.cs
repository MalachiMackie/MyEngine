namespace MyEngine.SourceGenerator.Tests.EcsEngineSourceBuilderTests;

[UsesVerify]
public class SystemInstantiationTests
{
    [Fact]
    public async Task Should_BuildFullSystemInstantiation()
    {
        var constructor = new SystemConstructor()
            .WithParameter(new SystemConstructorResourceParameter("MyResource1<string>"))
            .WithParameter(new SystemConstructorQueryParameter(
                new QueryComponentTypeParameter("MyQueryComponent1<bool>", null),
                [
                    new QueryComponentTypeParameter("MyQueryComponent2", MetaComponentType.OptionalComponent),
                    new QueryComponentTypeParameter("MyQueryComponent3<string>", null),
                    new QueryComponentTypeParameter("MyQueryComponent4", MetaComponentType.OptionalComponent)
                ]))
            .WithParameter(new SystemConstructorResourceParameter("MyResource2"))
            .WithParameter(new SystemConstructorQueryParameter(new QueryComponentTypeParameter("OtherQueryComponent", null), []));

        var systemClass = new SystemClass("MySystemClass<string>", constructor);

        var output = EcsEngineGlueSourceBuilder.BuildSystemInstantiation(systemClass);

        await Verify(output);
    }
}

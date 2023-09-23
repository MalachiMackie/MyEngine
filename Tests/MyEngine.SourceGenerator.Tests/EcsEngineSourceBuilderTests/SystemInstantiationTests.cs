namespace MyEngine.SourceGenerator.Tests.EcsEngineSourceBuilderTests;

public class SystemInstantiationTests
{

    [Fact]
    public void Should_BuildFullSystemInstantiation()
    {
        var constructor = new SystemConstructorDto
        {
            ResourceParameters = new[]
            {
                new SystemConstructorResourceParameterDto{Name = "MyResource1<string>", ParameterIndex = 0},
                new SystemConstructorResourceParameterDto{Name = "MyResource2", ParameterIndex = 2}
            },
            QueryParameters = new[]
            {
                new SystemConstructorQueryParameterDto
                {
                    ParameterIndex = 1,
                    TypeParameters = new []
                    {
                        new QueryComponentTypeParameterDto { ComponentTypeName = "MyQueryComponent1<bool>", MetaComponentType = null },
                        new QueryComponentTypeParameterDto { ComponentTypeName = "MyQueryComponent2", MetaComponentType = MetaComponentType.OptionalComponent },
                        new QueryComponentTypeParameterDto { ComponentTypeName = "MyQueryComponent3<string>", MetaComponentType = null },
                        new QueryComponentTypeParameterDto { ComponentTypeName = "MyQueryComponent4", MetaComponentType = MetaComponentType.OptionalComponent },

                    }
                },
                new SystemConstructorQueryParameterDto
                {
                    ParameterIndex = 3,
                    TypeParameters = new []
                    {
                        new QueryComponentTypeParameterDto { ComponentTypeName = "OtherQueryComponent", MetaComponentType = null }
                    }
                }
            }
        };
        
        var systemClass = new SystemClassDto{
            FullyQualifiedName = "MySystemClass<string>",
            Constructor = constructor
        };

        var output = EcsEngineSourceBuilder.BuildSystemInstantiation(systemClass);

        output.Should().Be(
            """
            _systemInstantiations.Add(typeof(global::MySystemClass<string>), () =>
            {
                global::MyEngine.Core.Ecs.Components.EntityComponents<global::MyQueryComponent1<bool>,
                                                                      global::MyEngine.Core.Ecs.Components.OptionalComponent<global::MyQueryComponent2>,
                                                                      global::MyQueryComponent3<string>,
                                                                      global::MyEngine.Core.Ecs.Components.OptionalComponent<global::MyQueryComponent4>>? GetQuery1Components(global::MyEngine.Core.Ecs.EntityId entityId)
                {
                    if (_components.TryGetComponent<global::MyQueryComponent1<bool>>(entityId, out var component1)
                        && _components.TryGetComponent<global::MyQueryComponent3<string>>(entityId, out var component3))
                    {
                        var component2 = _components.GetOptionalComponent<global::MyQueryComponent2>(entityId);
                        var component4 = _components.GetOptionalComponent<global::MyQueryComponent4>(entityId);
                        return new global::MyEngine.Core.Ecs.Components.EntityComponents<global::MyQueryComponent1<bool>,
                                                                                         global::MyEngine.Core.Ecs.Components.OptionalComponent<global::MyQueryComponent2>,
                                                                                         global::MyQueryComponent3<string>,
                                                                                         global::MyEngine.Core.Ecs.Components.OptionalComponent<global::MyQueryComponent4>>(entityId)
                        {
                            Component1 = component1,
                            Component2 = component2,
                            Component3 = component3,
                            Component4 = component4
                        };
                    }
                    return null;
                }
                global::MyEngine.Core.Ecs.Components.EntityComponents<global::OtherQueryComponent>? GetQuery2Components(global::MyEngine.Core.Ecs.EntityId entityId)
                {
                    if (_components.TryGetComponent<global::OtherQueryComponent>(entityId, out var component1))
                    {
                        
                        return new global::MyEngine.Core.Ecs.Components.EntityComponents<global::OtherQueryComponent>(entityId)
                        {
                            Component1 = component1
                        };
                    }
                    return null;
                }
                if (_resourceContainer.TryGetResource<global::MyResource1<string>>(out var resource0)
                    && _resourceContainer.TryGetResource<global::MyResource2>(out var resource1))
                {
                    return new global::MySystemClass<string>(
                        resource0,
                        global::MyEngine.Runtime.Query.Create(_components, _entities, GetQuery1Components),
                        resource1,
                        global::MyEngine.Runtime.Query.Create(_components, _entities, GetQuery2Components));
                }

                return null;
            });

            """);
    }
}

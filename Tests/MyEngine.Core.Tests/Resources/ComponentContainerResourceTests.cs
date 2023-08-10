using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Core.Tests.Resources;

public class ComponentContainerResourceTests
{
    private readonly ComponentContainerResource _componentContainerResource = new();

    [Fact]
    public void AddComponent_Should_AddComponentsInCorrectOrder()
    {
        var entityId = EntityId.Generate();
        var component1 = new MyComponent1();
        var component2 = new MyComponent2();

        _componentContainerResource.AddComponent(entityId, component1);
        _componentContainerResource.AddComponent(entityId, component2);

        _componentContainerResource.NewComponents.Should()
            .BeEquivalentTo(new (EntityId, IComponent)[] {
                (entityId, component1),
                (entityId, component2)
            });
    }

    [Fact]
    public void RemoveComponent_Should_RemoveComponentsInCorrectOrder()
    {
        var entityId1 = EntityId.Generate();
        var entityId2 = EntityId.Generate();
        _componentContainerResource.RemoveComponent<MyComponent1>(entityId1);
        _componentContainerResource.RemoveComponent<MyComponent1>(entityId2);
        _componentContainerResource.RemoveComponent<MyComponent2>(entityId1);

        _componentContainerResource.RemoveComponents.Should().BeEquivalentTo(new[] {
            (entityId1, typeof(MyComponent1)),
            (entityId2, typeof(MyComponent1)),
            (entityId1, typeof(MyComponent2)),
        });
    }
}

file class MyComponent1 : IComponent
{
}

file class MyComponent2 : IComponent
{
}

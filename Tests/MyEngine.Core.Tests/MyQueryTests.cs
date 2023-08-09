using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Tests;

public class MyQueryTests
{
    [Fact]
    public void SingleComponentQuery_Should_BeAnEnumerableOfComponents()
    {
        var component1 = new MyComponent();
        var component2 = new MyComponent();
        var query = new MyQuery<MyComponent>(() => new[] {
            component1,
            component2
        });

        var result = query.ToList();

        result.Should().BeEquivalentTo(new[] { component1, component2 });
    }

    [Fact]
    public void SingleComponentQuery_Should_EvaluateComponentsForEachEnumeration()
    {
        var evaluations = 0;
        var query = new MyQuery<MyComponent>(() =>
        {
            evaluations++;
            return Array.Empty<MyComponent>();
        });

        _ = query.ToList();
        _ = query.ToList();

        evaluations.Should().Be(2);
    }
}

file class MyComponent : IComponent
{
    public EntityId EntityId { get; set; } = null!;
}

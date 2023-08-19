using System.Diagnostics.CodeAnalysis;

namespace MyEngine.Core.Ecs.Components;

public class OptionalComponent<T> : IComponent
    where T : IComponent
{
    public T? Component { get; }

    [MemberNotNullWhen(true, nameof(Component))]
    public bool HasComponent => Component is not null;

    public OptionalComponent(T? component) 
    {
        Component = component;
    }
}

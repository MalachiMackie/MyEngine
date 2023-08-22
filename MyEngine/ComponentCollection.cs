using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;

namespace MyEngine.Runtime;

internal class ComponentCollection
{
    private readonly Dictionary<Type, Dictionary<EntityId, IComponent>> _components = new();

    public bool DoesEntityHaveComponent<TComponent>(EntityId entityId)
        where TComponent : IComponent
    {
        return _components.TryGetValue(typeof(TComponent), out var components)
            && components.ContainsKey(entityId);
    }

    public void AddComponent(EntityId entityId, IComponent component)
    {
        var type = component.GetType();
        if (!_components.TryGetValue(type, out var components))
        {
            components = new();
            _components[type] = components;
        }

        if (components.ContainsKey(entityId))
        {
            throw new InvalidOperationException($"Component has already been added");
        }

        components.Add(entityId, component);
    }

    public void DeleteComponentsForEntity(EntityId entityId)
    {
        foreach (var (_, components) in _components)
        {
            components.Remove(entityId);
        }
    }

    public void DeleteComponent(EntityId entityId, Type componentType)
    {
        if (_components.TryGetValue(componentType, out var entityComponents))
        {
            entityComponents.Remove(entityId);
        }
    }

    public void DeleteComponent<T>(EntityId entityId)
        where T : IComponent
    {
        DeleteComponent(entityId, typeof(T));
    }

    public bool TryGetComponent<TComponent>(EntityId entityId, [NotNullWhen(true)] out TComponent? component)
        where TComponent : IComponent
    {
        if (!_components.TryGetValue(typeof(TComponent), out var components))
        {
            component = default;
            return false;
        }

        if (components.TryGetValue(entityId, out var innerComponent))
        {
            if (innerComponent is not TComponent tComponent)
            {
                throw new UnreachableException();
            }

            component = tComponent;
            return true;
        }

        component = default;
        return false;
    }

    public OptionalComponent<TComponent> GetOptionalComponent<TComponent>(EntityId entityId)
        where TComponent : class, IComponent
    {
        if (TryGetComponent<TComponent>(entityId, out var foundComponent))
        {
            return new OptionalComponent<TComponent>(foundComponent);
        }

        return new OptionalComponent<TComponent>(null);
    }
}

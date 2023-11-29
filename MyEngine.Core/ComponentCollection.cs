using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Utils;

namespace MyEngine.Core;

internal class ComponentCollection
{
    private readonly Dictionary<Type, Dictionary<EntityId, IComponent>> _components = new();

    public bool DoesEntityHaveComponent<TComponent>(EntityId entityId)
        where TComponent : IComponent
    {
        return _components.TryGetValue(typeof(TComponent), out var components)
            && components.ContainsKey(entityId);
    }

    public Result<Unit> AddComponent(EntityId entityId, IComponent component)
    {
        var type = component.GetType();
        if (!_components.TryGetValue(type, out var components))
        {
            components = new();
            _components[type] = components;
        }

        if (components.ContainsKey(entityId))
        {
            return Result.Failure<Unit>($"Entity {entityId} already contains a component of type {type.Name}");
        }

        components.Add(entityId, component);
        return Result.Success<Unit>(Unit.Value);
    }

    public void DeleteAllComponentsForEntity(EntityId entityId)
    {
        foreach (var (_, components) in _components)
        {
            components.Remove(entityId);
        }
    }

    public bool DeleteComponent(EntityId entityId, Type componentType)
    {
        if (_components.TryGetValue(componentType, out var entityComponents))
        {
            return entityComponents.Remove(entityId);
        }

        return false;
    }

    public bool DeleteComponent<T>(EntityId entityId)
        where T : IComponent
    {
        return DeleteComponent(entityId, typeof(T));
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

    public EntityComponents<T>? TryGetComponentsForEntity<T>(EntityId entityId)
        where T : IComponent
    {
        if (TryGetComponent<T>(entityId, out var component))
        {
            return new EntityComponents<T>(entityId) { Component1 = component };
        }

        return null;
    }

    public EntityComponents<T1, T2>? TryGetComponentsForEntity<T1, T2>(EntityId entityId)
        where T1 : IComponent
        where T2 : IComponent
    {
        if (TryGetComponent<T1>(entityId, out var component1)
            && TryGetComponent<T2>(entityId, out var component2))
        {
            return new EntityComponents<T1, T2>(entityId)
            {
                Component1 = component1,
                Component2 = component2
            };
        }

        return null;
    }

    public EntityComponents<T1, T2, T3>? TryGetComponentsForEntity<T1, T2, T3>(EntityId entityId)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        if (TryGetComponent<T1>(entityId, out var component1)
            && TryGetComponent<T2>(entityId, out var component2)
            && TryGetComponent<T3>(entityId, out var component3))
        {
            return new EntityComponents<T1, T2, T3>(entityId)
            {
                Component1 = component1,
                Component2 = component2,
                Component3 = component3
            };
        }

        return null;
    }

    public EntityComponents<T1, T2, T3, T4>? TryGetComponentsForEntity<T1, T2, T3, T4>(EntityId entityId)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        if (TryGetComponent<T1>(entityId, out var component1)
            && TryGetComponent<T2>(entityId, out var component2)
            && TryGetComponent<T3>(entityId, out var component3)
            && TryGetComponent<T4>(entityId, out var component4))
        {
            return new EntityComponents<T1, T2, T3, T4>(entityId)
            {
                Component1 = component1,
                Component2 = component2,
                Component3 = component3,
                Component4 = component4,
            };
        }

        return null;
    }

    public EntityComponents<T1, T2, T3, T4, T5>? TryGetComponentsForEntity<T1, T2, T3, T4, T5>(EntityId entityId)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        if (TryGetComponent<T1>(entityId, out var component1)
            && TryGetComponent<T2>(entityId, out var component2)
            && TryGetComponent<T3>(entityId, out var component3)
            && TryGetComponent<T4>(entityId, out var component4)
            && TryGetComponent<T5>(entityId, out var component5))
        {
            return new EntityComponents<T1, T2, T3, T4, T5>(entityId)
            {
                Component1 = component1,
                Component2 = component2,
                Component3 = component3,
                Component4 = component4,
                Component5 = component5,
            };
        }

        return null;
    }

    public EntityComponents<T1, T2, T3, T4, T5, T6>? TryGetComponentsForEntity<T1, T2, T3, T4, T5, T6>(EntityId entityId)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        if (TryGetComponent<T1>(entityId, out var component1)
            && TryGetComponent<T2>(entityId, out var component2)
            && TryGetComponent<T3>(entityId, out var component3)
            && TryGetComponent<T4>(entityId, out var component4)
            && TryGetComponent<T5>(entityId, out var component5)
            && TryGetComponent<T6>(entityId, out var component6))
        {
            return new EntityComponents<T1, T2, T3, T4, T5, T6>(entityId)
            {
                Component1 = component1,
                Component2 = component2,
                Component3 = component3,
                Component4 = component4,
                Component5 = component5,
                Component6 = component6,
            };
        }

        return null;
    }
}

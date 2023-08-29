using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Utils;

namespace MyEngine.Runtime;

internal enum AddComponentError
{
    DuplicateComponent    
}

internal class ComponentCollection
{
    private readonly Dictionary<Type, Dictionary<EntityId, IComponent>> _components = new();

    public bool DoesEntityHaveComponent<TComponent>(EntityId entityId)
        where TComponent : IComponent
    {
        return _components.TryGetValue(typeof(TComponent), out var components)
            && components.ContainsKey(entityId);
    }

    public Result<Unit, AddComponentError> AddComponent(EntityId entityId, IComponent component)
    {
        var type = component.GetType();
        if (!_components.TryGetValue(type, out var components))
        {
            components = new();
            _components[type] = components;
        }

        if (components.ContainsKey(entityId))
        {
            return Result.Failure<Unit, AddComponentError>(AddComponentError.DuplicateComponent);
        }

        components.Add(entityId, component);
        return Result.Success<Unit, AddComponentError>(Unit.Value);
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
}

using System.Diagnostics;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyEngine.Runtime;

internal class Commands : ICommands
{
    private readonly ICollection<EntityId> _entities;
    private readonly ComponentCollection _componentCollection;

    public Commands(ComponentCollection componentCollection, ICollection<EntityId> entities)
    {
        _componentCollection = componentCollection;
        _entities = entities;
    }

    public Result<Unit, AddComponentCommandError> AddComponent(EntityId entityId, IComponent component)
    {
        if (!_entities.Contains(entityId))
        {
            return Result.Failure<Unit, AddComponentCommandError>(AddComponentCommandError.EntityDoesNotExist);
        }

        return _componentCollection.AddComponent(entityId, component)
            .MapError(err => err switch
            {
                AddComponentError.DuplicateComponent => AddComponentCommandError.DuplicateComponent,
                _ => throw new UnreachableException() 
            });
    }

    public Result<EntityId, AddEntityCommandError> CreateEntity(Transform transform, params IComponent[] components)
    {
        var entityId = EntityId.Generate();
        _componentCollection.AddComponent(entityId, new TransformComponent(transform));
        foreach (var component in components)
        {
            if (_componentCollection.AddComponent(entityId, component).TryGetError(out var addComponentError))
            {
                _componentCollection.DeleteAllComponentsForEntity(entityId);
                return addComponentError switch
                {
                    AddComponentError.DuplicateComponent => Result.Failure<EntityId, AddEntityCommandError>(AddEntityCommandError.DuplicateComponent),
                    _ => throw new UnreachableException()
                };
            }
        }

        _entities.Add(entityId);

        return Result.Success<EntityId, AddEntityCommandError>(entityId);
    }

    public Result<Unit, RemoveEntityCommandError> RemoveEntity(EntityId entityId)
    {
        if (!_entities.Remove(entityId))
        {
            return Result.Failure<Unit, RemoveEntityCommandError>(RemoveEntityCommandError.EntityDoesNotExist);
        }

        // todo: check and remove entity as parent or child

        return Result.Success<Unit, RemoveEntityCommandError>(Unit.Value);
    }

    public bool RemoveComponent<T>(EntityId entityId) where T : IComponent
    {
        // todo: check and remove entity as parent or child
        return _componentCollection.DeleteComponent<T>(entityId);
    }
}

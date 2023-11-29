using System.Diagnostics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyEngine.Core;

internal class Commands : ICommands
{
    private readonly ICollection<EntityId> _entities;
    private readonly ComponentCollection _componentCollection;

    public Commands(ComponentCollection componentCollection, ICollection<EntityId> entities)
    {
        _componentCollection = componentCollection;
        _entities = entities;
    }

    public Result<Unit> AddComponent(EntityId entityId, IComponent component)
    {
        if (!_entities.Contains(entityId))
        {
            return Result.Failure<Unit>($"Entity with id {entityId} does not exist");
        }

        return _componentCollection.AddComponent(entityId, component);
    }

    public Result<EntityId> CreateEntity(Transform transform, params IComponent[] components)
    {
        return CreateEntity(components.Append(new TransformComponent(transform)));
    }

    private Result<EntityId> CreateEntity(IEnumerable<IComponent> components)
    {
        var entityId = EntityId.Generate();
        foreach (var component in components)
        {
            var addResult = _componentCollection.AddComponent(entityId, component);
            if (addResult.IsFailure)
            {
                // clean up any components that were already added
                _componentCollection.DeleteAllComponentsForEntity(entityId);
                return Result.Failure<EntityId, Unit>(addResult);
            }
        }

        _entities.Add(entityId);

        return Result.Success<EntityId>(entityId);
    }

    public Result<Unit> RemoveEntity(EntityId entityId)
    {
        if (!_entities.Remove(entityId))
        {
            return Result.Failure<Unit>($"Entity with id {entityId} does not exist");
        }

        // todo: check and remove entity as parent or child

        return Result.Success<Unit>(Unit.Value);
    }

    public bool RemoveComponent<T>(EntityId entityId) where T : IComponent
    {
        // todo: check and remove entity as parent or child
        return _componentCollection.DeleteComponent<T>(entityId);
    }

    public Result<Unit> AddChild(EntityId parentId, EntityId childId)
    {
        if (_componentCollection.TryGetComponent<ParentComponent>(childId, out _))
        {
            return Result.Failure<Unit>($"Cannot add {parentId} as a parent to {childId}, as {childId} already has a parent");
        }

        if (!_componentCollection.TryGetComponent<ChildrenComponent>(parentId, out var childrenComponent))
        {
            childrenComponent = new ChildrenComponent();
            _componentCollection.AddComponent(parentId, childrenComponent);
        }

        var validateResult = ValidateCircularReference(parentId, childId);
        if (validateResult.IsFailure)
        {
            return Result.Failure<Unit>($"Cannot add {parentId} as a parent to {childId} due to a circular reference");
        }

        childrenComponent.AddChild(childId);

        _componentCollection.AddComponent(childId, new ParentComponent(parentId));

        _componentCollection.TryGetComponent<TransformComponent>(parentId, out var parentTransform);
        _componentCollection.TryGetComponent<TransformComponent>(childId, out var childTransform);

        childTransform!.GlobalTransform.SetWithParentTransform(parentTransform!.GlobalTransform, childTransform.LocalTransform);

        return Result.Success<Unit>(Unit.Value);
    }

    private Result<Unit> ValidateCircularReference(EntityId parentId, EntityId childId)
    {
        if (!_componentCollection.TryGetComponent<ParentComponent>(parentId, out var parentComponent))
        {
            // parent has no parent, so no circular reference found
            return Result.Success<Unit>(Unit.Value);
        }

        if (parentComponent.Parent == childId)
        {
            return Result.Failure<Unit>();
        }

        return ValidateCircularReference(parentComponent.Parent, childId);
    }

    /// <summary>
    /// Remove a child from its parent, while keeping the child global transform in the same position
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="childId"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void RemoveChild(EntityId parentId, EntityId childId)
    {
        _componentCollection.DeleteComponent<ParentComponent>(childId);

        // sync global transform with local transform now that child no longer has a parent
        _componentCollection.TryGetComponent<TransformComponent>(childId, out var childTransform);
        childTransform!.GlobalTransform.SetWithTransform(childTransform.LocalTransform);
    }

    public Result<EntityId> CreateEntity(Transform transform, IEnumerable<IComponent> components, IEnumerable<EntityId> children)
    {
        var addEntityResult = CreateEntity(components.Append(new TransformComponent(transform)));
        if (!addEntityResult.TryGetValue(out var entityId))
        {
            return Result.Failure<EntityId, EntityId>(addEntityResult);
        }

        foreach (var child in children)
        {
            var addChildResult = AddChild(entityId, child);
            if (addChildResult.IsFailure)
            {
                return Result.Failure<EntityId, Unit>(addChildResult);
            }
        }

        return Result.Success<EntityId>(entityId);
    }

    public Result<EntityId> CreateEntity(Func<ITransformStepEntityBuilder, ICompleteStepEntityBuilder> entityBuilder)
    {
        return CreateEntity(entityBuilder(EntityBuilder.Create()).Build(), new());
    }

    private Result<EntityId> CreateEntity(ICompleteStepEntityBuilder.BuildResult buildResult, List<EntityId> createdEntities)
    {
        var createResult = CreateEntity(buildResult.Components);
        if (!createResult.TryGetValue(out var entityId))
        {
            return createResult;
        }
        createdEntities.Add(entityId);

        Result<EntityId>? failedResult = null;

        foreach (var child in buildResult.Children.Select(x => x.Build()))
        {
            var createChildResult = CreateEntity(child, createdEntities);
            if (createChildResult.IsFailure)
            {
                failedResult = createChildResult;
                break;
            }
            AddChild(entityId, createChildResult.Unwrap())
                .Expect("Creating child should not fail as entity builder can only be used with new entities");
        }

        if (failedResult is null)
        {
            return createResult;
        }

        while (createdEntities.Count > 0)
        {
            var createdEntity = createdEntities[0];
            createdEntities.RemoveAt(0);
            _componentCollection.DeleteAllComponentsForEntity(createdEntity);
            _entities.Remove(createdEntity);
        }

        return Result.Failure<EntityId, EntityId>(failedResult.Value);
    }
}

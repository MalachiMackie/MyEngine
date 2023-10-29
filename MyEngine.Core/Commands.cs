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
        return CreateEntity(components.Append(new TransformComponent(transform)));
    }

    private Result<EntityId, AddEntityCommandError> CreateEntity(IEnumerable<IComponent> components)
    {
        var entityId = EntityId.Generate();
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

    public Result<Unit, AddChildError> AddChild(EntityId parentId, EntityId childId)
    {
        if (_componentCollection.TryGetComponent<ParentComponent>(childId, out _))
        {
            return Result.Failure<Unit, AddChildError>(AddChildError.ChildAlreadyHasParent);
        }

        if (!_componentCollection.TryGetComponent<ChildrenComponent>(parentId, out var childrenComponent))
        {
            childrenComponent = new ChildrenComponent();
            _componentCollection.AddComponent(parentId, childrenComponent);
        }

        var validateResult = ValidateCircularReference(parentId, childId);
        if (validateResult.IsFailure)
        {
            return validateResult.MapError(_ => AddChildError.CircularReference);
        }

        childrenComponent.AddChild(childId);

        _componentCollection.AddComponent(childId, new ParentComponent(parentId));

        _componentCollection.TryGetComponent<TransformComponent>(parentId, out var parentTransform);
        _componentCollection.TryGetComponent<TransformComponent>(childId, out var childTransform);

        childTransform!.GlobalTransform.SetWithParentTransform(parentTransform!.GlobalTransform, childTransform.LocalTransform);

        return Result.Success<Unit, AddChildError>(Unit.Value);
    }

    private Result<Unit, Unit> ValidateCircularReference(EntityId parentId, EntityId childId)
    {
        if (!_componentCollection.TryGetComponent<ParentComponent>(parentId, out var parentComponent))
        {
            // parent has no parent, so no circular reference found
            return Result.Success<Unit, Unit>(Unit.Value);
        }

        if (parentComponent.Parent == childId)
        {
            return Result.Failure<Unit, Unit>(Unit.Value);
        }

        return ValidateCircularReference(parentComponent.Parent, childId);
    }

    /// <summary>
    /// Add child to the parent, while keeping its global transform in the same position. If there is non-uniform scaling, this won't produce expected results
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="childId"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public Result<Unit, AddChildInPlaceError> AddChildInPlace(EntityId parentId, EntityId childId)
    {
        if (_componentCollection.TryGetComponent<ParentComponent>(childId, out _))
        {
            return Result.Failure<Unit, AddChildInPlaceError>(AddChildInPlaceError.ChildAlreadyHasParent);
        }

        if (!_componentCollection.TryGetComponent<ChildrenComponent>(parentId, out var childrenComponent))
        {
            childrenComponent = new ChildrenComponent();
            _componentCollection.AddComponent(parentId, childrenComponent);
        }

        var validateResult = ValidateCircularReference(parentId, childId);
        if (validateResult.IsFailure)
        {
            return validateResult.MapError(_ => AddChildInPlaceError.CircularReference);
        }

        childrenComponent.AddChild(childId);

        _componentCollection.AddComponent(childId, new ParentComponent(parentId));
        AddChild(parentId, childId);

        // update local transform to keep global transform in place
        if (!_componentCollection.TryGetComponent<TransformComponent>(parentId, out var parentTransformComponent))
        {
            throw new InvalidOperationException("Entity is missing a transform");
        }

        if (!_componentCollection.TryGetComponent<TransformComponent>(childId, out var childTransformComponent))
        {
            throw new InvalidOperationException("Entity is missing a transform");
        }

        // child can't have any parent, so treat its transform as global
        var desiredGlobal = GlobalTransform.FromTransform(childTransformComponent.LocalTransform);

        var parentGlobal = parentTransformComponent.GlobalTransform;

        if (!Matrix4x4.Invert(parentGlobal.ModelMatrix, out var inverseParent))
        {
            return Result.Failure<Unit, AddChildInPlaceError>(AddChildInPlaceError.UnableToCalculateRelativeLocalTransform);
        }

        var local = desiredGlobal.ModelMatrix * inverseParent;

        if (!Matrix4x4.Decompose(local, out var localScale, out var localRotation, out var localPosition))
        {
            MathHelper.NormalizeMatrix(ref local);
            if (!Matrix4x4.Decompose(local, out localScale, out localRotation, out localPosition))
            {
                return Result.Failure<Unit, AddChildInPlaceError>(AddChildInPlaceError.UnableToCalculateRelativeLocalTransform);
            }
        }

        childTransformComponent.LocalTransform.position = localPosition;
        childTransformComponent.LocalTransform.rotation = localRotation;
        childTransformComponent.LocalTransform.scale = localScale;

        return Result.Success<Unit, AddChildInPlaceError>(Unit.Value);
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

    public Result<EntityId, OneOf<AddEntityCommandError, AddChildError>> CreateEntity(Transform transform, IEnumerable<IComponent> components, IEnumerable<EntityId> children)
    {
        var addEntityResult = CreateEntity(components.Append(new TransformComponent(transform)));
        if (!addEntityResult.TryGetValue(out var entityId))
        {
            return Result.Failure<EntityId, OneOf<AddEntityCommandError, AddChildError>>(new (addEntityResult.UnwrapError()));
        }

        foreach (var child in children)
        {
            var addChildResult = AddChild(entityId, child);
            if (addChildResult.TryGetError(out var error))
            {
                return Result.Failure<EntityId, OneOf<AddEntityCommandError, AddChildError>>(new (error));
            }
        }

        return Result.Success<EntityId, OneOf<AddEntityCommandError, AddChildError>>(entityId);
    }

    public Result<EntityId, AddEntityCommandError> CreateEntity(Func<ITransformStepEntityBuilder, ICompleteStepEntityBuilder> entityBuilder)
    {
        return CreateEntity(entityBuilder(EntityBuilder.Create()).Build(), new());
    }

    private Result<EntityId, AddEntityCommandError> CreateEntity(ICompleteStepEntityBuilder.BuildResult buildResult, List<EntityId> createdEntities)
    {
        var createResult = CreateEntity(buildResult.Components);
        if (!createResult.TryGetValue(out var entityId))
        {
            return createResult;
        }
        createdEntities.Add(entityId);

        AddEntityCommandError? error = null;

        foreach (var child in buildResult.Children.Select(x => x.Build()))
        {
            var createChildResult = CreateEntity(child, createdEntities);
            if (createChildResult.TryGetError(out var childError))
            {
                error = childError;
                break;
            }
            AddChild(entityId, createChildResult.Unwrap())
                .Expect("Creating child should not fail as entity builder can only be used with new entities");
        }

        if (!error.HasValue)
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

        return Result.Failure<EntityId, AddEntityCommandError>(error.Value);
    }
}

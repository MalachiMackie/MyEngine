using MyEngine.Core.Ecs.Components;
using MyEngine.Utils;

namespace MyEngine.Core.Ecs.Resources;

public enum AddComponentCommandError
{
    DuplicateComponent,
    EntityDoesNotExist
}

public enum AddEntityCommandError
{
    DuplicateComponent
}


public enum RemoveEntityCommandError
{
    EntityDoesNotExist
}

public enum AddChildError
{
    ChildAlreadyHasParent,
    CircularReference
}

public enum AddChildInPlaceError
{
    ChildAlreadyHasParent,
    CircularReference,
    UnableToCalculateRelativeLocalTransform
}

public interface ICommands : IResource
{
    Result<EntityId, AddEntityCommandError> CreateEntity(Transform transform, params IComponent[] components);
    Result<EntityId, AddEntityCommandError> CreateEntity(Func<ITransformStepEntityBuilder, ICompleteStepEntityBuilder> entityBuilder);

    Result<EntityId, OneOf<AddEntityCommandError, AddChildError>> CreateEntity(Transform transform, IEnumerable<IComponent> components, IEnumerable<EntityId> children);

    Result<Unit, AddComponentCommandError> AddComponent(EntityId entityId, IComponent component);

    bool RemoveComponent<T>(EntityId entityId)
        where T : IComponent;

    Result<Unit, RemoveEntityCommandError> RemoveEntity(EntityId brick);

    Result<Unit, AddChildInPlaceError> AddChildInPlace(EntityId parentId, EntityId childId);

    Result<Unit, AddChildError> AddChild(EntityId parentId, EntityId childId);

    void RemoveChild(EntityId parentId, EntityId childId);
}


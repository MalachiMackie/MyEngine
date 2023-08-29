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

public interface ICommands : IResource
{
    Result<EntityId, AddEntityCommandError> CreateEntity(Func<IEntityBuilderTransformStep, IEntityBuilder> entityBuilderFunc);

    Result<Unit, AddComponentCommandError> AddComponent(EntityId entityId, IComponent component);

    bool RemoveComponent<T>(EntityId entityId)
        where T : IComponent;

    Result<Unit, RemoveEntityCommandError> RemoveEntity(EntityId brick);
}


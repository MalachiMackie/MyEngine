using MyEngine.Core.Ecs.Components;
using MyEngine.Utils;

namespace MyEngine.Core.Ecs.Resources;

public interface ICommands : IResource
{
    Result<EntityId> CreateEntity(Transform transform, params IComponent[] components);
    Result<EntityId> CreateEntity(Func<ITransformStepEntityBuilder, ICompleteStepEntityBuilder> entityBuilder);

    Result<EntityId> CreateEntity(Transform transform, IEnumerable<IComponent> components, IEnumerable<EntityId> children);

    Result<Unit> AddComponent(EntityId entityId, IComponent component);

    bool RemoveComponent<T>(EntityId entityId)
        where T : IComponent;

    Result<Unit> RemoveEntity(EntityId brick);

    Result<Unit> AddChild(EntityId parentId, EntityId childId);

    void RemoveChild(EntityId parentId, EntityId childId);
}


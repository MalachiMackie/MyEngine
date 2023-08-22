using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Runtime;

internal class EntityCommands : IEntityCommands
{
    private readonly ICollection<EntityId> _entities;
    private readonly ComponentCollection _componentCollection;

    public EntityCommands(ComponentCollection componentCollection, ICollection<EntityId> entities)
    {
        _componentCollection = componentCollection;
        _entities = entities;
    }

    public EntityId AddEntity()
    {
        return AddEntity(TransformComponent.DefaultTransform());
    }

    public EntityId AddEntity(Transform transform)
    {
        var entityId = EntityId.Generate();
        _entities.Add(entityId);
        _componentCollection.AddComponent(entityId, new TransformComponent(transform));

        return entityId;
    }

    public void RemoveEntity(EntityId entityId)
    {
        _componentCollection.DeleteComponentsForEntity(entityId);
        _entities.Remove(entityId);
    }
}

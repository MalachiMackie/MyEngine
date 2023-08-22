using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;

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

    public void AddComponent(EntityId entityId, IComponent component)
    {
        _componentCollection.AddComponent(entityId, component);
    }

    public EntityId AddEntity(Func<IEntityBuilderTransformStep, IEntityBuilder> entityBuilderFunc)
    {
        var entityBuilder = EntityBuilder.Create();
        var result = entityBuilderFunc(entityBuilder);
        var components = result.Build();

        var entityId = EntityId.Generate();
        _entities.Add(entityId);
        foreach (var component in components)
        {
            _componentCollection.AddComponent(entityId, component);
        }

        return entityId;
    }

    public void RemoveEntity(EntityId entityId)
    {
        _componentCollection.DeleteComponentsForEntity(entityId);
        _entities.Remove(entityId);
    }
}

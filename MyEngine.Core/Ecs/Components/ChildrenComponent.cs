namespace MyEngine.Core.Ecs.Components;

public class ChildrenComponent : IComponent
{
    private readonly List<EntityId> _children = new();

    public IReadOnlyCollection<EntityId> Children => _children;

    internal void AddChild(EntityId entityId)
    {
        _children.Add(entityId);
    }

    internal void RemoveChild(EntityId entityId)
    {
        _children.Remove(entityId);
    }
}

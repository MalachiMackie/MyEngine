using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;

namespace MyEngine.Runtime;

internal class HierarchyCommands : IHierarchyCommands
{
    private readonly ComponentCollection _componentCollection;

    public HierarchyCommands(ComponentCollection componentCollection)
    {
        _componentCollection = componentCollection;
    }

    public void AddChild(EntityId parentId, EntityId childId)
    {
        if (_componentCollection.TryGetComponent<ParentComponent>(childId, out _))
        {
            throw new InvalidOperationException("child already has a parent");
        }

        if (!_componentCollection.TryGetComponent<ChildrenComponent>(parentId, out var childrenComponent))
        {
            childrenComponent = new ChildrenComponent();
            _componentCollection.AddComponent(parentId, childrenComponent);
        }
        childrenComponent.AddChild(childId);

        _componentCollection.AddComponent(childId, new ParentComponent(parentId));

        // todo: traverse down the tree and adjust global transforms
    }

    public void RemoveChild(EntityId parentId, EntityId childId)
    {
        _componentCollection.DeleteComponent<ParentComponent>(childId);
        if (_componentCollection.TryGetComponent<ChildrenComponent>(parentId, out var childrenComponent))
        {
            childrenComponent.RemoveChild(childId);
        }

        // todo: traverse down the tree and adjust global transforms 
    }
}

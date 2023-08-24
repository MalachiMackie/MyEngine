using System.Numerics;
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

        if (!_componentCollection.TryGetComponent<TransformComponent>(parentId, out var parentTransformComponent))
        {
            throw new InvalidOperationException("Entity is missing a transform");
        }

        if (!_componentCollection.TryGetComponent<TransformComponent>(childId, out var childTransformComponent))
        {
            throw new InvalidOperationException("Entity is missing a transform");
        }

        // update local transform so that global transform stays the same
        var localPosition = childTransformComponent.GlobalTransform.position - parentTransformComponent.GlobalTransform.position;
        // apply global rotation, then inverse parent rotation
        // quaternions get applied/rotated right to left
        // localRotation = parentRotation.Inversed * globalRotation
        var localRotation = Quaternion.Inverse(parentTransformComponent.GlobalTransform.rotation) * childTransformComponent.GlobalTransform.rotation;
        var localScale = childTransformComponent.GlobalTransform.scale / parentTransformComponent.GlobalTransform.scale;

        childTransformComponent.LocalTransform.position = localPosition;
        childTransformComponent.LocalTransform.rotation = localRotation;
        childTransformComponent.LocalTransform.scale = localScale;

    }

    public void RemoveChild(EntityId parentId, EntityId childId)
    {
        _componentCollection.DeleteComponent<ParentComponent>(childId);
        if (_componentCollection.TryGetComponent<ChildrenComponent>(parentId, out var childrenComponent))
        {
            childrenComponent.RemoveChild(childId);
        }

        if (!_componentCollection.TryGetComponent<TransformComponent>(childId, out var transformComponent))
        {
            throw new InvalidOperationException("Entity is missing a transform");
        }

        // set child's local transform to be equal to their global transform
        transformComponent.LocalTransform.position = transformComponent.GlobalTransform.position;
        transformComponent.LocalTransform.rotation = transformComponent.GlobalTransform.rotation;
        transformComponent.LocalTransform.scale = transformComponent.GlobalTransform.scale;

    }
}

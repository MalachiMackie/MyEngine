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

    }

    /// <summary>
    /// Add child to the parent, while keeping its global transform in the same position. If there is non-uniform scaling, this won't produce expected results
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="childId"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void AddChildInPlace(EntityId parentId, EntityId childId)
    {
        throw new NotImplementedException();

        /* AddChild(parentId, childId);

        // update local transform to keep global transform in place
        if (!_componentCollection.TryGetComponent<TransformComponent>(parentId, out var parentTransformComponent))
        {
            throw new InvalidOperationException("Entity is missing a transform");
        }

        if (!_componentCollection.TryGetComponent<TransformComponent>(childId, out var childTransformComponent))
        {
            throw new InvalidOperationException("Entity is missing a transform");
        }

        // todo: verify we dont add a circular dependency here

        // child can't have any parent, so treat its transform as global
        var desiredGlobal = GlobalTransform.FromTransform(childTransformComponent.LocalTransform);

        var parentGlobal = parentTransformComponent.GlobalTransform;

        if (!Matrix4x4.Invert(parentGlobal.ModelMatrix, out var inverseParent))
        {
            throw new InvalidOperationException("Something failed");
        }

        var local = desiredGlobal.ModelMatrix * inverseParent;

        if (!Matrix4x4.Decompose(local, out var localScale, out var localRotation, out var localPosition))
        {
            MathHelper.NormalizeMatrix(ref local);
            if (!Matrix4x4.Decompose(local, out localScale, out localRotation, out localPosition))
            {
                throw new InvalidOperationException("Something failed");
            }
        }

        childTransformComponent.LocalTransform.position = localPosition;
        childTransformComponent.LocalTransform.rotation = localRotation;
        childTransformComponent.LocalTransform.scale = localScale;

        */
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
    }
}

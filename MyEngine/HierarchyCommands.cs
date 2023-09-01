using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyEngine.Runtime;

internal class HierarchyCommands : IHierarchyCommands
{
    private readonly ComponentCollection _componentCollection;

    public HierarchyCommands(ComponentCollection componentCollection)
    {
        _componentCollection = componentCollection;
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
}

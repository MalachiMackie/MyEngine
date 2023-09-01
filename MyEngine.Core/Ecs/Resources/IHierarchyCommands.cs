using MyEngine.Utils;

namespace MyEngine.Core.Ecs.Resources;

public interface IHierarchyCommands : IResource
{
    public Result<Unit, AddChildInPlaceError> AddChildInPlace(EntityId parentId, EntityId childId);

    public Result<Unit, AddChildError> AddChild(EntityId parentId, EntityId childId);

    public void RemoveChild(EntityId parentId, EntityId childId);

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

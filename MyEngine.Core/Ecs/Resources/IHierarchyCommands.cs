using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyEngine.Core.Ecs;

public interface IHierarchyCommands : IResource
{
    public Result<Unit, AddChildInPlaceError> AddChildInPlace(EntityId parentId, EntityId childId);

    public Result<Unit, AddChildError> AddChild(EntityId parentId, EntityId childId);

    public void RemoveChild(EntityId parentId, EntityId childId);

}

public enum AddChildError {
    ChildAlreadyHasParent,
    CircularReference
}

public enum AddChildInPlaceError
{
    ChildAlreadyHasParent,
    CircularReference,
    UnableToCalculateRelativeLocalTransform
}

using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Core.Ecs;

public interface IHierarchyCommands : IResource
{
    public void AddChildInPlace(EntityId parentId, EntityId childId);

    public void AddChild(EntityId parentId, EntityId childId);

    public void RemoveChild(EntityId parentId, EntityId childId);
}

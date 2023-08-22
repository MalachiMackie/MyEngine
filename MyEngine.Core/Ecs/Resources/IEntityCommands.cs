namespace MyEngine.Core.Ecs.Resources;

public interface IEntityCommands : IResource
{
    public EntityId AddEntity();
    public EntityId AddEntity(Transform transform);
}

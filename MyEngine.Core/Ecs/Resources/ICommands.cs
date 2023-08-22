using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Ecs.Resources;

public interface ICommands : IResource
{
    public EntityId AddEntity(Func<IEntityBuilderTransformStep, IEntityBuilder> entityBuilderFunc);

    public void AddComponent(EntityId entityId, IComponent component);
}


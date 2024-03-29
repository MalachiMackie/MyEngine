namespace MyEngine.Core.Ecs.Components;

public class ParentComponent : IComponent
{
    public ParentComponent(EntityId parent)
    {
        Parent = parent;
    }

    public EntityId Parent { get; set; }
}

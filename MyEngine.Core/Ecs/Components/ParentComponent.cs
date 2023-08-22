namespace MyEngine.Core.Ecs.Components;

public class ParentComponent : IComponent
{
    internal ParentComponent(EntityId parent)
    {
        Parent = parent;
    }

    public EntityId Parent { get; internal set; }
}

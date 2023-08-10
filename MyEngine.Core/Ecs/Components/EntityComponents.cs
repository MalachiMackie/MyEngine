namespace MyEngine.Core.Ecs.Components;

public class EntityComponents
{
    protected EntityComponents(EntityId entityId)
    {
        EntityId = entityId;
    }

    public EntityId EntityId { get; }
}

public class EntityComponents<T> : EntityComponents
    where T : IComponent
{
    internal EntityComponents(EntityId entityId) : base(entityId)
    {
    }

    public required T Component { get; init; }

    public static implicit operator T (EntityComponents<T> component)
    {
        return component.Component;
    }
}

public class EntityComponents<T1, T2> : EntityComponents
    where T1 : IComponent
    where T2 : IComponent
{
    internal EntityComponents(EntityId entityId) : base(entityId) { }

    public required T1 Component1 { get; init; }
    public required T2 Component2 { get; init; }

    public void Deconstruct(out T1 component1, out T2 component2)
    {
        component1 = Component1;
        component2 = Component2;
    }
}

public class EntityComponents<T1, T2, T3> : EntityComponents
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
{
    internal EntityComponents(EntityId entityId) : base(entityId)
    {

    }

    public required T1 Component1 { get; init; }
    public required T2 Component2 { get; init; }
    public required T3 Component3 { get; init; }

    public void Deconstruct(out T1 component1, out T2 component2, out T3 component3)
    {
        component1 = Component1;
        component2 = Component2;
        component3 = Component3;
    }

}

public class EntityComponents<T1, T2, T3, T4> : EntityComponents
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    internal EntityComponents(EntityId entityId) : base(entityId)
    {

    }

    public required T1 Component1 { get; init; }
    public required T2 Component2 { get; init; }
    public required T3 Component3 { get; init; }
    public required T4 Component4 { get; init; }
    public void Deconstruct(out T1 component1, out T2 component2, out T3 component3, out T4 component4)
    {
        component1 = Component1;
        component2 = Component2;
        component3 = Component3;
        component4 = Component4;
    }
}

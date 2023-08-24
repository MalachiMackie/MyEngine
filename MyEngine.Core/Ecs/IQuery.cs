using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Ecs;

public interface IQuery {}

public interface IQuery<T> : IEnumerable<EntityComponents<T>>, IQuery
    where T : IComponent
{
    EntityComponents<T>? TryGetForEntity(EntityId entityId);
}

public interface IQuery<T1, T2> : IEnumerable<EntityComponents<T1, T2>>, IQuery
    where T1 : IComponent
    where T2 : IComponent
{
    EntityComponents<T1, T2>? TryGetForEntity(EntityId entityId);
}


public interface IQuery<T1, T2, T3> : IEnumerable<EntityComponents<T1, T2, T3>>,  IQuery
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
{
    EntityComponents<T1, T2, T3>? TryGetForEntity(EntityId entityId);
}

public interface IQuery<T1, T2, T3, T4> : IEnumerable<EntityComponents<T1, T2, T3, T4>>, IQuery
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    EntityComponents<T1, T2, T3, T4>? TryGetForEntity(EntityId entityId);
}

public interface IQuery<T1, T2, T3, T4, T5> : IEnumerable<EntityComponents<T1, T2, T3, T4, T5>>, IQuery
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
{
    EntityComponents<T1, T2, T3, T4, T5>? TryGetForEntity(EntityId entityId);
}

using System.Collections;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;

namespace MyEngine.Runtime;

internal class Query<T> : IQuery<T>
    where T : IComponent
{
    public required Func<IEnumerable<EntityComponents<T>>> GetAllImpl { get; init; }
    public required Func<EntityId, EntityComponents<T>?> TryGetForEntityImpl { get; init; }

    public IEnumerable<EntityComponents<T>> GetAll() => GetAllImpl();

    public EntityComponents<T>? TryGetForEntity(EntityId entityId) => TryGetForEntityImpl(entityId);

    public IEnumerator<EntityComponents<T>> GetEnumerator()
    {
        var enumerable = GetAllImpl();
        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal class Query<T1, T2> : IQuery<T1, T2>
    where T1 : IComponent
    where T2 : IComponent
{
    public required Func<IEnumerable<EntityComponents<T1, T2>>> GetAllImpl { get; init; }
    public required Func<EntityId, EntityComponents<T1, T2>?> TryGetForEntityImpl { get; init; }

    public EntityComponents<T1, T2>? TryGetForEntity(EntityId entityId) => TryGetForEntityImpl(entityId);

    public IEnumerator<EntityComponents<T1, T2>> GetEnumerator()
    {
        var enumerable = GetAllImpl();
        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal class Query<T1, T2, T3> : IQuery<T1, T2, T3>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
{
    public required Func<IEnumerable<EntityComponents<T1, T2, T3>>> GetAllImpl { get; init; }
    public required Func<EntityId, EntityComponents<T1, T2, T3>?> TryGetForEntityImpl { get; init; }

    public EntityComponents<T1, T2, T3>? TryGetForEntity(EntityId entityId) => TryGetForEntityImpl(entityId);

    public IEnumerator<EntityComponents<T1, T2, T3>> GetEnumerator()
    {
        var enumerable = GetAllImpl();
        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal class Query<T1, T2, T3, T4> : IQuery<T1, T2, T3, T4>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    public required Func<IEnumerable<EntityComponents<T1, T2, T3, T4>>> GetAllImpl { get; init; }
    public required Func<EntityId, EntityComponents<T1, T2, T3, T4>?> TryGetForEntityImpl { get; init; }

    public EntityComponents<T1, T2, T3, T4>? TryGetForEntity(EntityId entityId) => TryGetForEntityImpl(entityId);

    public IEnumerator<EntityComponents<T1, T2, T3, T4>> GetEnumerator()
    {
        var enumerable = GetAllImpl();
        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

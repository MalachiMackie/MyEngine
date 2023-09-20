using System.Collections;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Utils;

namespace MyEngine.Runtime;

internal static class Query
{
    public static IQuery<T> Create<T>(ComponentCollection componentCollection, ISet<EntityId> entities, Func<EntityId, EntityComponents<T>?>? tryGetForEntity = null)
        where T : IComponent
    {
        tryGetForEntity ??= componentCollection.TryGetComponentsForEntity<T>;
        return new Query<T>()
        {
            GetAllImpl = () => entities.Select(tryGetForEntity).WhereNotNull(),
            TryGetForEntityImpl = tryGetForEntity
        };
    }

    public static IQuery<T1, T2> Create<T1, T2>(ComponentCollection componentCollection, ISet<EntityId> entities, Func<EntityId, EntityComponents<T1, T2>?>? tryGetForEntity = null)
        where T1 : IComponent
        where T2 : IComponent
    {
        tryGetForEntity ??= componentCollection.TryGetComponentsForEntity<T1, T2>;
        return new Query<T1, T2>()
        {
            GetAllImpl = () => entities.Select(tryGetForEntity).WhereNotNull(),
            TryGetForEntityImpl = tryGetForEntity
        };
    }

    public static IQuery<T1, T2, T3> Create<T1, T2, T3>(ComponentCollection componentCollection, ISet<EntityId> entities, Func<EntityId, EntityComponents<T1, T2, T3>?>? tryGetForEntity = null)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        tryGetForEntity ??= componentCollection.TryGetComponentsForEntity<T1, T2, T3>;
        return new Query<T1, T2, T3>()
        {
            GetAllImpl = () => entities.Select(tryGetForEntity).WhereNotNull(),
            TryGetForEntityImpl = tryGetForEntity
        };
    }

    public static IQuery<T1, T2, T3, T4> Create<T1, T2, T3, T4>(ComponentCollection componentCollection, ISet<EntityId> entities, Func<EntityId, EntityComponents<T1, T2, T3, T4>?>? tryGetForEntity = null)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        tryGetForEntity ??= componentCollection.TryGetComponentsForEntity<T1, T2, T3, T4>;
        return new Query<T1, T2, T3, T4>()
        {
            GetAllImpl = () => entities.Select(tryGetForEntity).WhereNotNull(),
            TryGetForEntityImpl = tryGetForEntity
        };
    }

    public static IQuery<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(ComponentCollection componentCollection, ISet<EntityId> entities, Func<EntityId, EntityComponents<T1, T2, T3, T4, T5>?>? tryGetForEntity = null)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        tryGetForEntity ??= componentCollection.TryGetComponentsForEntity<T1, T2, T3, T4, T5>;
        return new Query<T1, T2, T3, T4, T5>()
        {
            GetAllImpl = () => entities.Select(tryGetForEntity).WhereNotNull(),
            TryGetForEntityImpl = tryGetForEntity
        };
    }
}

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

internal class Query<T1, T2, T3, T4, T5> : IQuery<T1, T2, T3, T4, T5>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
{
    public required Func<IEnumerable<EntityComponents<T1, T2, T3, T4, T5>>> GetAllImpl { get; init; }
    public required Func<EntityId, EntityComponents<T1, T2, T3, T4, T5>?> TryGetForEntityImpl { get; init; }

    public EntityComponents<T1, T2, T3, T4, T5>? TryGetForEntity(EntityId entityId) => TryGetForEntityImpl(entityId);

    public IEnumerator<EntityComponents<T1, T2, T3, T4, T5>> GetEnumerator()
    {
        var enumerable = GetAllImpl();
        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

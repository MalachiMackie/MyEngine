using System.Diagnostics;
using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Utils;

namespace MyEngine.Physics;

/// <summary>
/// This bounce system exists because Bepu doesn't support Restitution Coefficient https://github.com/bepu/bepuphysics2/issues/52.
/// Instead this is a hack that manually 'bounces' dynamic bodies when they encounter a collision
/// </summary>
public class BounceSystem : ISystem
{
    // todo: push colliders back apart?

    private readonly IQuery<
        OptionalComponent<DynamicBody2DComponent>,
        OptionalComponent<KinematicBody2DComponent>,
        OptionalComponent<StaticBody2DComponent>,
        OptionalComponent<VelocityComponent>,
        OptionalComponent<BouncinessComponent>
    > _bodyQuery;
    private readonly CollisionsResource _collisionsResource;
    private readonly PhysicsResource _physicsResource;

    public BounceSystem(
        CollisionsResource collisionsResource,
        IQuery<OptionalComponent<DynamicBody2DComponent>, OptionalComponent<KinematicBody2DComponent>, OptionalComponent<StaticBody2DComponent>, OptionalComponent<VelocityComponent>, OptionalComponent<BouncinessComponent>> bodyQuery,
        PhysicsResource physicsResource)
    {
        _collisionsResource = collisionsResource;
        _bodyQuery = bodyQuery;
        _physicsResource = physicsResource;
    }

    public void Run(double deltaTime)
    {
        // todo: test assumption that we don't need to group by EntityB as well?
        var groupedCollisions = _collisionsResource.NewCollisions.GroupBy(x => x.EntityA);

        foreach (var grouping in groupedCollisions)
        {
            HandleCollisionGrouping(grouping.Key, grouping);
        }
    }

    private void HandleCollisionGrouping(EntityId entity, IEnumerable<Collision> collisions)
    {
        var bodyResult = _bodyQuery.TryGetForEntity(entity)
            ?? throw new UnreachableException("Impossible unless the entity doesn't exist");

        var (maybeDynamicComponent, maybeKinematicComponent, maybeStaticComponent, maybeVelocityComponent, maybeBouncinessComponent) = bodyResult;
        if (maybeDynamicComponent.HasComponent)
        {
            HandleDynamicCollision(
                new DynamicProperties(
                    bodyResult.EntityId,
                    maybeDynamicComponent.Component,
                    maybeVelocityComponent.Component?.Velocity,
                    maybeBouncinessComponent.Component?.Bounciness),
                collisions);
        }
        else if (maybeKinematicComponent.HasComponent)
        {
            HandleKinematicCollision(
                new KinematicProperties(
                    maybeKinematicComponent.Component,
                    maybeVelocityComponent.Component?.Velocity,
                    maybeBouncinessComponent.Component?.Bounciness),
                collisions);
        }
        else if (maybeStaticComponent.HasComponent)
        {
            HandleStaticCollision(
                new StaticProperties(
                    maybeStaticComponent.Component,
                    maybeBouncinessComponent.Component?.Bounciness),
                collisions);
        }
        else
        {
            Console.WriteLine("Collision was generated for entity that didn't have either dynamic or kinematic body component??");
            return;
        }
    }

    private void HandleDynamicCollision(DynamicProperties dynamicBody, IEnumerable<Collision> collisions)
    {
        Vector3? dynamicVelocity = null;
        var collisionBodies = collisions.Select(x => (queryResult: _bodyQuery.TryGetForEntity(x.EntityB), collision: x))
            .Where(x => x.queryResult?.Component1 is not null || x.queryResult?.Component2 is not null || x.queryResult?.Component3 is not null)
            .Select(x => {
                dynamicVelocity ??= x.collision.EntityAVelocity;

                var (maybeDynamic, maybeKinematic, maybeStatic, maybeVelocity, maybeBounciness) = x.queryResult!;
                if (maybeDynamic.HasComponent)
                {
                    return (new OneOf<DynamicProperties, KinematicProperties, StaticProperties>(
                        new DynamicProperties(
                            x.queryResult.EntityId,
                            maybeDynamic.Component,
                            maybeVelocity.Component?.Velocity,
                            maybeBounciness.Component?.Bounciness)), x.collision);
                }

                if (maybeKinematic.HasComponent)
                {
                    return (new OneOf<DynamicProperties, KinematicProperties, StaticProperties>(
                        new KinematicProperties(
                            maybeKinematic.Component,
                            maybeVelocity.Component?.Velocity,
                            maybeBounciness.Component?.Bounciness)), x.collision);
                }

                if (maybeStatic.HasComponent)
                {
                    return (new OneOf<DynamicProperties, KinematicProperties, StaticProperties>(
                        new StaticProperties(
                            maybeStatic.Component,
                            maybeBounciness.Component?.Bounciness)), x.collision);
                }

                throw new UnreachableException();
            })
            .ToArray();

        if (collisionBodies.Length == 0)
        {
            // nothing to bounce with
            return;
        }

        if (collisionBodies.Length == 1)
        {
            var (body, collision) = collisionBodies[0];

            body.Match(
                x => HandleDynamicToDynamicSingleCollision(dynamicBody, collision.EntityAVelocity!.Value, x, collision.EntityBVelocity!.Value, collision.Normal),
                x => HandleDynamicToKinematicSingleCollision(dynamicBody, collision.EntityAVelocity!.Value, x, collision.EntityBVelocity!.Value, collision.Normal),
                x => HandleDynamicToStaticSingleCollision(dynamicBody, collision.EntityAVelocity!.Value, x, collision.Normal));

            return;
        }

        HandleDynamicToManyCollisions(dynamicBody, collisionBodies);
    }

    private void HandleKinematicCollision(KinematicProperties kinematicBody, IEnumerable<Collision> collisions)
    {
        var otherComponents = collisions.Select(x => (queryResult: _bodyQuery.TryGetForEntity(x.EntityB), collision: x))
            // only dynamic bodies can collide with kinematic bodies
            .Where(x => x.queryResult?.Component1.Component is not null) // todo: this property naming scheme isn't refactor friendly
            .ToArray(); 

        if (otherComponents.Length == 0)
        {
            // none of the bodies we collided with were dynamic bodies
            return;
        }

        if (otherComponents.Length == 1)
        {
            var (queryResult, collision) = otherComponents[0];
            var (maybeDynamic, maybeKinematic, maybeStatic, maybeVelocity, maybeBounciness) = queryResult!;
            HandleDynamicToKinematicSingleCollision(
                new DynamicProperties(
                    queryResult.EntityId,
                    maybeDynamic.Component ?? throw new UnreachableException(), // this was checked at the top of the method
                    maybeVelocity.Component?.Velocity,
                    maybeBounciness.Component?.Bounciness),
                collision.EntityBVelocity ?? throw new UnreachableException(),
                kinematicBody,
                collision.EntityAVelocity ?? throw new UnreachableException(),
                collision.Normal);
            return;
        }

        // each of these components has a dynamic body, so handle each of their collisions individually
        foreach (var (queryResult, collision) in otherComponents)
        {
            var (maybeDynamic, maybeKinematic, maybeStatic, maybeVelocity, maybeBounciness) = queryResult!;
            HandleDynamicToKinematicSingleCollision(
                new DynamicProperties(
                    queryResult.EntityId,
                    maybeDynamic.Component ?? throw new UnreachableException(),
                    maybeVelocity.Component?.Velocity,
                    maybeBounciness.Component?.Bounciness),
                collision.EntityBVelocity ?? throw new UnreachableException(),
                kinematicBody,
                collision.EntityAVelocity ?? throw new UnreachableException(),
                collision.Normal);
        }
    }

    private void HandleStaticCollision(StaticProperties staticProps, IEnumerable<Collision> collisions)
    {
        var otherComponents = collisions.Select(x => (queryResult: _bodyQuery.TryGetForEntity(x.EntityB), collision: x))
            // only dynamic bodies can collide with static bodies
            .Where(x => x.queryResult?.Component1 is not null) // todo: this property naming scheme isn't refactor friendly
            .ToArray(); 

        if (otherComponents.Length == 0)
        {
            // none of the bodies we collided with were dynamic bodies
            return;
        }

        if (otherComponents.Length == 1)
        {
            var (queryResult, collision) = otherComponents[0];
            var (maybeDynamic, maybeKinematic, maybeStatic, maybeVelocity, maybeBounciness) = queryResult!;

            HandleDynamicToStaticSingleCollision(
                new DynamicProperties(
                    queryResult.EntityId,
                    maybeDynamic.Component ?? throw new UnreachableException(), // this was checked at the top of the method
                    maybeVelocity.Component?.Velocity,
                    maybeBounciness.Component?.Bounciness),
                collision.EntityBVelocity ?? throw new UnreachableException("Entity B is a dynamic body, which has to have velocity"),
                staticProps,
                collision.Normal);
            return;
        }

        // each of these components has a dynamic body, so handle each of their collisions individually
        foreach (var (queryResult, collision) in otherComponents)
        {
            var (maybeDynamic, maybeKinematic, maybeStatic, maybeVelocity, maybeBounciness) = queryResult!;
            HandleDynamicToStaticSingleCollision(
                new DynamicProperties(
                    queryResult.EntityId,
                    maybeDynamic.Component ?? throw new UnreachableException(),
                    maybeVelocity.Component?.Velocity,
                    maybeBounciness.Component?.Bounciness),
                collision.EntityBVelocity ?? throw new UnreachableException("Entity B is a dynamic body, which has to have velocity"),
                staticProps,
                collision.Normal);
        }
    }

    private record struct KinematicProperties(KinematicBody2DComponent KinematicBody, Vector3? CurrentVelocity, float? Bounciness);
    private record struct DynamicProperties(EntityId EntityId, DynamicBody2DComponent DynamicBody, Vector3? CurrentVelocity, float? Bounciness);
    private record struct StaticProperties(StaticBody2DComponent StaticBody, float? Bounciness);

    private void HandleDynamicToKinematicSingleCollision(DynamicProperties dynamicProps, Vector3 dynamicVelocity, KinematicProperties kinematicProps, Vector3 kinematicVelocity, Vector3 normal)
    {
        var maxBounciness = MathF.Max(dynamicProps.Bounciness ?? 0f, kinematicProps.Bounciness ?? 0f);
        if (maxBounciness <= 0f)
        {
            return;
        }

        // todo: take into account kinematic velocity

        var reboundedVelocity = GetReboundedVelocity(dynamicVelocity.XY(), normal.XY());
        var currentVelocity = GetDynamicVelocity(dynamicProps);

        // todo: this combining is incorrect
        var endVelocity = (reboundedVelocity * maxBounciness) + (currentVelocity.XY() * (1f - maxBounciness));

        _physicsResource.SetBody2DVelocity(dynamicProps.EntityId, endVelocity);
    }

    private void HandleDynamicToStaticSingleCollision(DynamicProperties dynamicProps, Vector3 dynamicVelocity, StaticProperties staticProps, Vector3 normal)
    {
        var maxBounciness = MathF.Max(dynamicProps.Bounciness ?? 0f, staticProps.Bounciness ?? 0f);
        if (maxBounciness <= 0f)
        {
            return;
        }

        var reboundedVelocity = GetReboundedVelocity(dynamicVelocity.XY(), normal.XY());
        var currentVelocity = GetDynamicVelocity(dynamicProps);

        // todo: this combining is incorrect
        var endVelocity = (reboundedVelocity * maxBounciness) + (currentVelocity.XY() * (1f - maxBounciness));

        _physicsResource.SetBody2DVelocity(dynamicProps.EntityId, endVelocity);
    }

    private void HandleDynamicToDynamicSingleCollision(
        DynamicProperties dynamicPropsA,
        Vector3 dynamicVelocityA,
        DynamicProperties dynamicPropsB,
        Vector3 dynamicVelocityB,
        Vector3 normal)
    {
        throw new NotImplementedException();
    }

    private void HandleDynamicToManyCollisions(
        DynamicProperties dynamicProps,
        IReadOnlyList<(OneOf<DynamicProperties, KinematicProperties, StaticProperties> Body, Collision Collision)> otherBodies)
    {
        // todo: don't only generate bounce on first collision
        var (body, collision) = otherBodies[0];
        body.Match(
            x => HandleDynamicToDynamicSingleCollision(dynamicProps, collision.EntityAVelocity!.Value, x, collision.EntityBVelocity!.Value, collision.Normal),
            x => HandleDynamicToKinematicSingleCollision(dynamicProps, collision.EntityAVelocity!.Value, x, collision.EntityBVelocity!.Value, collision.Normal),
            x => HandleDynamicToStaticSingleCollision(dynamicProps, collision.EntityAVelocity!.Value, x, collision.Normal));
    }

    private static Vector2 GetReboundedVelocity(Vector2 currentVelocity, Vector2 collisionNormal)
    {
        if (currentVelocity.Length() < 0.0001f)
        {
            return Vector2.Zero;
        }

        // r=d−2(d * n)n
        // d = direction
        // n = normal
        // r = reflection

        var normalizedVelocity = Vector2.Normalize(currentVelocity);
        var normalizedNormal = Vector2.Normalize(collisionNormal);

        var dot = Vector2.Dot(normalizedVelocity, normalizedNormal);
        var reflection = normalizedVelocity - 2 * dot * normalizedNormal;

        return reflection * currentVelocity.Length();
    }

    private Vector3 GetDynamicVelocity(DynamicProperties dynamicProps)
    {
        return dynamicProps.CurrentVelocity ?? _physicsResource.GetCurrentVelocity(dynamicProps.EntityId).Unwrap();
    }
}

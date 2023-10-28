using System.Diagnostics;
using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Utils;

using BodyQuery = MyEngine.Core.Ecs.IQuery<
        MyEngine.Core.Ecs.Components.OptionalComponent<MyEngine.Physics.DynamicBody2DComponent>,
        MyEngine.Core.Ecs.Components.OptionalComponent<MyEngine.Physics.KinematicBody2DComponent>,
        MyEngine.Core.Ecs.Components.OptionalComponent<MyEngine.Physics.StaticBody2DComponent>,
        MyEngine.Core.Ecs.Components.OptionalComponent<MyEngine.Physics.BouncinessComponent>
    >;

namespace MyEngine.Physics;

/// <summary>
/// This bounce system exists because Bepu doesn't support Restitution Coefficient https://github.com/bepu/bepuphysics2/issues/52.
/// Instead this is a hack that manually 'bounces' dynamic bodies when they encounter a collision
/// </summary>
public class BounceSystem : ISystem
{
    // todo: push colliders back apart?

    private readonly BodyQuery _bodyQuery;
    private readonly CollisionsResource _collisionsResource;
    private readonly PhysicsResource _physicsResource;

    public BounceSystem(
        CollisionsResource collisionsResource,
        BodyQuery bodyQuery,
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

        var (maybeDynamicComponent, maybeKinematicComponent, maybeStaticComponent, maybeBouncinessComponent) = bodyResult;
        if (maybeDynamicComponent.HasComponent)
        {
            HandleDynamicCollision(
                new DynamicProperties(
                    bodyResult.EntityId,
                    maybeDynamicComponent.Component,
                    maybeBouncinessComponent.Component?.Bounciness),
                collisions);
        }
        else if (maybeKinematicComponent.HasComponent)
        {
            HandleKinematicCollision(
                new KinematicProperties(
                    maybeKinematicComponent.Component,
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

                var (maybeDynamic, maybeKinematic, maybeStatic, maybeBounciness) = x.queryResult!;
                if (maybeDynamic.HasComponent)
                {
                    return (new OneOf<DynamicProperties, KinematicProperties, StaticProperties>(
                        new DynamicProperties(
                            x.queryResult.EntityId,
                            maybeDynamic.Component,
                            maybeBounciness.Component?.Bounciness)), x.collision);
                }

                if (maybeKinematic.HasComponent)
                {
                    return (new OneOf<DynamicProperties, KinematicProperties, StaticProperties>(
                        new KinematicProperties(
                            maybeKinematic.Component,
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
            var (maybeDynamic, maybeKinematic, maybeStatic, maybeBounciness) = queryResult!;
            HandleDynamicToKinematicSingleCollision(
                new DynamicProperties(
                    queryResult.EntityId,
                    maybeDynamic.Component ?? throw new UnreachableException(), // this was checked at the top of the method
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
            var (maybeDynamic, maybeKinematic, maybeStatic, maybeBounciness) = queryResult!;
            HandleDynamicToKinematicSingleCollision(
                new DynamicProperties(
                    queryResult.EntityId,
                    maybeDynamic.Component ?? throw new UnreachableException(),
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
            var (maybeDynamic, maybeKinematic, maybeStatic, maybeBounciness) = queryResult!;

            HandleDynamicToStaticSingleCollision(
                new DynamicProperties(
                    queryResult.EntityId,
                    maybeDynamic.Component ?? throw new UnreachableException(), // this was checked at the top of the method
                    maybeBounciness.Component?.Bounciness),
                collision.EntityBVelocity ?? throw new UnreachableException("Entity B is a dynamic body, which has to have velocity"),
                staticProps,
                collision.Normal);
            return;
        }

        // each of these components has a dynamic body, so handle each of their collisions individually
        foreach (var (queryResult, collision) in otherComponents)
        {
            var (maybeDynamic, maybeKinematic, maybeStatic, maybeBounciness) = queryResult!;
            HandleDynamicToStaticSingleCollision(
                new DynamicProperties(
                    queryResult.EntityId,
                    maybeDynamic.Component ?? throw new UnreachableException(),
                    maybeBounciness.Component?.Bounciness),
                collision.EntityBVelocity ?? throw new UnreachableException("Entity B is a dynamic body, which has to have velocity"),
                staticProps,
                collision.Normal);
        }
    }

    private record struct KinematicProperties(KinematicBody2DComponent KinematicBody, float? Bounciness);
    private record struct DynamicProperties(EntityId EntityId, DynamicBody2DComponent DynamicBody, float? Bounciness);
    private record struct StaticProperties(StaticBody2DComponent StaticBody, float? Bounciness);

    private void HandleDynamicToKinematicSingleCollision(DynamicProperties dynamicProps, Vector3 dynamicVelocity, KinematicProperties kinematicProps, Vector3 kinematicVelocity, Vector3 normal)
    {
        var maxBounciness = MathF.Max(dynamicProps.Bounciness ?? 0f, kinematicProps.Bounciness ?? 0f);
        if (maxBounciness < 0f)
        {
            Console.WriteLine("Bounciness cannot be less than 0");
            return;
        }

        if (maxBounciness < 0.01f)
        {
            _physicsResource.SetBody2DVelocity(dynamicProps.EntityId, Vector2.Zero);
            return;
        }

        var reboundedVelocity = GetReboundedVelocity(dynamicVelocity.XY(), normal.XY());

        var endVelocity = reboundedVelocity.WithMagnitude(reboundedVelocity.Length() * maxBounciness).Expect("Magnitude is greater than or equal to 0");

        _physicsResource.SetBody2DVelocity(dynamicProps.EntityId, endVelocity);
    }

    private void HandleDynamicToStaticSingleCollision(DynamicProperties dynamicProps, Vector3 dynamicVelocity, StaticProperties staticProps, Vector3 normal)
    {
        var maxBounciness = MathF.Max(dynamicProps.Bounciness ?? 0f, staticProps.Bounciness ?? 0f);
        if (maxBounciness < 0f)
        {
            Console.WriteLine("Bounciness cannot be less than 0");
            return;
        }

        if (maxBounciness < 0.01f)
        {
            _physicsResource.SetBody2DVelocity(dynamicProps.EntityId, Vector2.Zero);
            return;
        }

        var reboundedVelocity = GetReboundedVelocity(dynamicVelocity.XY(), normal.XY());

        var endVelocity = reboundedVelocity.WithMagnitude(reboundedVelocity.Length() * maxBounciness).Expect("Magnitude is greater than 0");

        _physicsResource.SetBody2DVelocity(dynamicProps.EntityId, endVelocity);
    }

    private void HandleDynamicToDynamicSingleCollision(
        DynamicProperties dynamicPropsA,
        Vector3 dynamicVelocityA,
        DynamicProperties dynamicPropsB,
        Vector3 dynamicVelocityB,
        Vector3 normal)
    {
        // todo: test this
        var maxBounciness = MathF.Max(dynamicPropsA.Bounciness ?? 0f, dynamicPropsB.Bounciness ?? 0f);

        if (maxBounciness <= 0f)
        {
            Console.WriteLine("Bounciness cannot be less than 0");
            return;
        }

        if (maxBounciness < 0.01f)
        {
            _physicsResource.SetBody2DVelocity(dynamicPropsA.EntityId, Vector2.Zero);
            _physicsResource.SetBody2DVelocity(dynamicPropsB.EntityId, Vector2.Zero);
            return;
        }

        var reboundedVelocityA = GetReboundedVelocity(dynamicVelocityA, normal);
        var reboundedVelocityB = GetReboundedVelocity(dynamicVelocityB, normal);

        _physicsResource.SetBody2DVelocity(
            dynamicPropsA.EntityId,
            reboundedVelocityA.WithMagnitude(dynamicVelocityB.Length() * maxBounciness).Expect("Magnitude is greater than 0").XY());

        _physicsResource.SetBody2DVelocity(
            dynamicPropsB.EntityId,
            reboundedVelocityB.WithMagnitude(dynamicVelocityA.Length() * maxBounciness).Expect("Magnitude is greater than 0").XY());
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
        return GetReboundedVelocity(currentVelocity.Extend(0f), collisionNormal.Extend(0f)).XY();
    }

    private static Vector3 GetReboundedVelocity(Vector3 currentVelocity, Vector3 collisionNormal)
    {
        if (currentVelocity.Length() < 0.0001f)
        {
            return Vector3.Zero;
        }

        // r=d−2(d * n)n
        // d = direction
        // n = normal
        // r = reflection

        var normalizedVelocity = Vector3.Normalize(currentVelocity);
        var normalizedNormal = Vector3.Normalize(collisionNormal);

        var dot = Vector3.Dot(normalizedVelocity, normalizedNormal);
        var reflection = normalizedVelocity - 2 * dot * normalizedNormal;

        return reflection * currentVelocity.Length();
    }
}

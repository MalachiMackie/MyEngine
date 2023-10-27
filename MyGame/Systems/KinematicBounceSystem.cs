using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Physics;
using MyEngine.Utils;
using MyGame.Components;

namespace MyGame.Systems;

public class KinematicBounceSystem : ISystem
{
    private readonly IQuery<KinematicBody2DComponent, VelocityComponent, OptionalComponent<KinematicReboundComponent>> _kinematicQuery;
    private readonly CollisionsResource _collisionsResource;
    private readonly PhysicsResource _physicsResource;

    public KinematicBounceSystem(
        IQuery<KinematicBody2DComponent, VelocityComponent, OptionalComponent<KinematicReboundComponent>> kinematicQuery,
        CollisionsResource collisionsResource,
        PhysicsResource physicsResource)
    {
        _kinematicQuery = kinematicQuery;
        _collisionsResource = collisionsResource;
        _physicsResource = physicsResource;
    }

    public void Run(double deltaTime)
    {
        var kinematicBodies = _kinematicQuery.ToDictionary(x => x.EntityId);
        foreach (var collision in _collisionsResource.NewCollisions)
        {
            var kinematicBodyComponentsA = _kinematicQuery.TryGetForEntity(collision.EntityA);
            var kinematicBodyComponentsB = _kinematicQuery.TryGetForEntity(collision.EntityB);

            if (kinematicBodyComponentsA is null && kinematicBodyComponentsB is null)
            {
                // neither bodies were kinematic, dont need to do any bouncing here
                continue;
            }

            if (kinematicBodyComponentsA is null || kinematicBodyComponentsB is null)
            {
                // only once is a kinematic body
                var (kinematicBody, velocityComponent, bounce) = (kinematicBodyComponentsA ?? kinematicBodyComponentsB)!;

                if (bounce.HasComponent)
                {
                    // only bounce the body if the bounce component exists
                    var velocity = GetReboundedVelocity(velocityComponent.Velocity.XY(), collision.Normal.XY());
                    _physicsResource.SetBody2DVelocity(kinematicBodyComponentsA?.EntityId ?? kinematicBodyComponentsB!.EntityId, velocity);
                }

                continue;
            }

            // both bodies are kinematic

            var (kinematicBodyA, velocityA, bounceA) = kinematicBodyComponentsA;
            var (kinematicBodyB, velocityB, bounceB) = kinematicBodyComponentsB;

            if (!bounceA.HasComponent && !bounceB.HasComponent)
            {
                // neither bodies have a bounce component. don't do anything
                continue;
            }

            if (bounceA.HasComponent && !bounceB.HasComponent)
            {
                // A has the bounce component
                var velocity = GetReboundedVelocity(velocityA.Velocity.XY(), collision.Normal.XY());
                _physicsResource.SetBody2DVelocity(kinematicBodyComponentsA.EntityId, velocity);
                continue;
            }

            if (bounceB.HasComponent && !bounceA.HasComponent)
            {
                // B has the bounce component
                var velocity = GetReboundedVelocity(velocityB.Velocity.XY(), collision.Normal.XY());
                _physicsResource.SetBody2DVelocity(kinematicBodyComponentsB.EntityId, velocity);
                continue;
            }

            // both A and B have bounce component

            // get the rebounded velocities for each body

            var aMagnitude = velocityA.Velocity.XY().Length();
            var bMagnitude = velocityB.Velocity.XY().Length();

            var aReboundedVelocity = GetReboundedVelocity(velocityA.Velocity.XY(), collision.Normal.XY());
            var bReboundedVelocity = GetReboundedVelocity(velocityB.Velocity.XY(), collision.Normal.XY());

            var aReboundMagnitude = aReboundedVelocity.Length();
            var bReboundMagnitude = bReboundedVelocity.Length();

            // assign the rebounded velocities, using the magnitude of the opposite body, as a crude transfer of momentum
            if (aMagnitude >= 0.0001f)
            {
                aReboundedVelocity.WithMagnitude(bReboundMagnitude).Match(
                    newVelocity => _physicsResource.SetBody2DVelocity(kinematicBodyComponentsA.EntityId, newVelocity),
                    err => Console.WriteLine("Failed to add set magnitude of vector: {0}", err));
            }
            else
            {
                // if we were previously still, we now want to be moving in the direction of the normal
                _physicsResource.SetBody2DVelocity(kinematicBodyComponentsA.EntityId, collision.Normal.XY() * bMagnitude);
            }

            if (bMagnitude >= 0.0001f)
            {
                bReboundedVelocity.WithMagnitude(aReboundMagnitude).Match(
                    newVelocity => _physicsResource.SetBody2DVelocity(kinematicBodyComponentsB.EntityId, newVelocity),
                    err => Console.WriteLine("Failed to add set magnitude of vector: {0}", err));
            }
            else
            {

                // if we were previously still, we now want to be moving in the opposite direction of the normal
                _physicsResource.SetBody2DVelocity(kinematicBodyComponentsB.EntityId, collision.Normal.XY() * aMagnitude);
            }
        }
    }

    public static Vector2 GetReboundedVelocity(Vector2 currentVelocity, Vector2 collisionNormal)
    {
        if (currentVelocity.Length() < 0.0001f)
        {
            return Vector2.Zero;
        }

        // r=d−2(d⋅n)n
        // d = direction
        // n = normal
        // r = reflection

        var normalizedVelocity = Vector2.Normalize(currentVelocity);
        var normalizedNormal = Vector2.Normalize(collisionNormal);

        var dot = Vector2.Dot(normalizedVelocity, normalizedNormal);
        var reflection = normalizedVelocity - 2 * dot * normalizedNormal;

        return reflection * currentVelocity.Length();
    }
}

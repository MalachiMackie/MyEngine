using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Utils;

namespace MyEngine.Physics;

public class KinematicBounceSystem : ISystem
{
    private readonly IQuery<KinematicBody2DComponent, OptionalComponent<KinematicReboundComponent>> _kinematicQuery;
    private readonly CollisionsResource _collisionsResource;

    public KinematicBounceSystem(
        IQuery<KinematicBody2DComponent, OptionalComponent<KinematicReboundComponent>> kinematicQuery,
        CollisionsResource collisionsResource)
    {
        _kinematicQuery = kinematicQuery;
        _collisionsResource = collisionsResource;
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
                var (kinematicBody, bounce) = (kinematicBodyComponentsA ?? kinematicBodyComponentsB)!; 

                if (bounce.HasComponent)
                {
                    // only bounce the body if the bounce component exists
                    kinematicBody.Velocity = GetReboundedVelocity(kinematicBody.Velocity, collision.Normal.XY());
                }

                continue;
            }

            // both bodies are kinematic

            var (kinematicBodyA, bounceA) = kinematicBodyComponentsA;
            var (kinematicBodyB, bounceB) = kinematicBodyComponentsB;

            if (!bounceA.HasComponent && !bounceB.HasComponent)
            {
                // neither bodies have a bounce component. don't do anything
                continue;
            }

            if (bounceA.HasComponent && !bounceB.HasComponent)
            {
                // A has the bounce component
                kinematicBodyA.Velocity = GetReboundedVelocity(kinematicBodyA.Velocity, collision.Normal.XY());
                continue;
            }

            if (bounceB.HasComponent && !bounceA.HasComponent)
            {
                // B has the bounce component
                kinematicBodyB.Velocity = GetReboundedVelocity(kinematicBodyB.Velocity, collision.Normal.XY());
                continue;
            }

            // both A and B have bounce component

            // get the rebounded velocities for each body 

            var aMagnitude = kinematicBodyA.Velocity.Length();
            var bMagnitude = kinematicBodyB.Velocity.Length();

            var aReboundedVelocity = GetReboundedVelocity(kinematicBodyA.Velocity, collision.Normal.XY());
            var bReboundedVelocity = GetReboundedVelocity(kinematicBodyB.Velocity, collision.Normal.XY());

            var aReboundMagnitude = aReboundedVelocity.Length();
            var bReboundMagnitude = bReboundedVelocity.Length();

            // assign the rebounded velocities, using the magnitude of the opposite body, as a crude transfer of momentum
            if (aMagnitude >= 0.0001f)
            {
                aReboundedVelocity.WithMagnitude(bReboundMagnitude).Match(
                    newVelocity => kinematicBodyA.Velocity = newVelocity,
                    err => Console.WriteLine("Failed to add set magnitude of vector: {0}", err));
            }
            else
            {
                // if we were previously still, we now want to be moving in the direction of the normal
                kinematicBodyA.Velocity = collision.Normal.XY() * bMagnitude;
            }

            if (bMagnitude >= 0.0001f)
            {
                bReboundedVelocity.WithMagnitude(aReboundMagnitude).Match(
                    newVelocity => kinematicBodyB.Velocity = newVelocity,
                    err => Console.WriteLine("Failed to add set magnitude of vector: {0}", err));
            }
            else
            {

                // if we were previously still, we now want to be moving in the opposite direction of the normal
                kinematicBodyB.Velocity = collision.Normal.XY() * -aMagnitude;
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
        var reflection = normalizedVelocity - (2 * dot * normalizedNormal);

        return reflection * currentVelocity.Length();
    }
}

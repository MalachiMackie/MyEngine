using System.Numerics;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Utils;

namespace MyEngine.Physics;

public class KinematicBounceSystem : ISystem
{
    private readonly IEnumerable<EntityComponents<KinematicBody2DComponent, KinematicReboundComponent>> _kinematicQuery;
    private readonly CollisionsResource _collisionsResource;

    public KinematicBounceSystem(
        IEnumerable<EntityComponents<KinematicBody2DComponent, KinematicReboundComponent>> kinematicQuery,
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
            var kinematicBodyA = kinematicBodies.GetValueOrDefault(collision.EntityA);
            var kinematicBodyB = kinematicBodies.GetValueOrDefault(collision.EntityB);

            if (!(kinematicBodyA is null ^ kinematicBodyB is null))
            {
                // either both bodies were kinematic, or neither were
                continue;
            }

            // todo: do bounce when both bodies are kinematic
            // this will mean the query Rebound component needs to be optional 
            // probably want a GetForEntity function for a given query.
            // That means we need to move back to a strongly typed query
            //
            // When we have two kinematic bodies that are both kinematic, we have 2 cases
            // case 1: only one is configured for bounce. That's easy
            // case 2: both have bounce. should we worry about mass? probably not.
            //     just bounce them according to their velocity, assuming equal mass

            var (kinematicBody, _) = (kinematicBodyA ?? kinematicBodyB)!;

            kinematicBody.Velocity = GetReboundedVelocity(kinematicBody.Velocity, collision.Normal.XY());

        }
    }

    public static Vector2 GetReboundedVelocity(Vector2 currentVelocity, Vector2 collisionNormal)
    {
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

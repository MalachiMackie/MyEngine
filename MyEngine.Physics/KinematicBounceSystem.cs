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

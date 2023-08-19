using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Physics;

public class KinematicVelocitySystem : ISystem
{
    private readonly IQuery<TransformComponent, KinematicBody2DComponent> _kinematicQuery;

    public KinematicVelocitySystem(IQuery<TransformComponent, KinematicBody2DComponent> kinematicQuery)
    {
        _kinematicQuery = kinematicQuery;
    }

    public void Run(double deltaTime)
    {
        foreach (var components in _kinematicQuery)
        {
            var (transform, body) = components;
            var deltaVelocity = body.Velocity * (float)deltaTime;
            transform.Transform.position += new Vector3(deltaVelocity.X, deltaVelocity.Y, 0f);
        }
    }
}

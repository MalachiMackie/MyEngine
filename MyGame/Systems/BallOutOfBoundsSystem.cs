using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyGame.Components;
using MyGame.Resources;

namespace MyGame.Systems;

public class BallOutOfBoundsSystem : ISystem
{
    private readonly IEnumerable<EntityComponents<TransformComponent, BallComponent, KinematicBody2DComponent>> _ballQuery;
    private readonly WorldSizeResource _worldSizeResource;

    public BallOutOfBoundsSystem(
        IEnumerable<EntityComponents<TransformComponent, BallComponent, KinematicBody2DComponent>> ballQuery,
        WorldSizeResource worldSizeResource)
    {
        _ballQuery = ballQuery;
        _worldSizeResource = worldSizeResource;
    }

    public void Run(double deltaTime)
    {
        foreach (var ballComponents in _ballQuery)
        {
            var (transform, _, kinematicBody2DComponent) = ballComponents;
            if (transform.Transform.position.X < _worldSizeResource.Left
                || transform.Transform.position.X > _worldSizeResource.Right
                || transform.Transform.position.Y < _worldSizeResource.Bottom
                || transform.Transform.position.Y > _worldSizeResource.Top)
            {
                Console.WriteLine("Ball was out of bounds {0}. Resetting", transform.Transform.position);

                kinematicBody2DComponent.Velocity = Vector2.Zero;
                transform.Transform.position = new Vector3(0f, -1f, 0f);
            }
        }
    }
}

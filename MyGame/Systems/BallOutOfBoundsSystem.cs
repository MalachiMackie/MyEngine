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
    private readonly IQuery<TransformComponent, BallComponent, KinematicBody2DComponent> _ballQuery;
    private readonly WorldSizeResource _worldSizeResource;

    public BallOutOfBoundsSystem(
        IQuery<TransformComponent, BallComponent, KinematicBody2DComponent> ballQuery,
        WorldSizeResource worldSizeResource)
    {
        _ballQuery = ballQuery;
        _worldSizeResource = worldSizeResource;
    }

    public void Run(double deltaTime)
    {
        foreach (var ballComponents in _ballQuery)
        {
            var (transformComponent, _, kinematicBody2DComponent) = ballComponents;
            var localTransform = transformComponent.LocalTransform;
            if (localTransform.position.X < _worldSizeResource.Left
                || localTransform.position.X > _worldSizeResource.Right
                || localTransform.position.Y < _worldSizeResource.Bottom
                || localTransform.position.Y > _worldSizeResource.Top)
            {
                Console.WriteLine("Ball was out of bounds {0}. Resetting", localTransform.position);

                kinematicBody2DComponent.Velocity = Vector2.Zero;
                localTransform.position = new Vector3(0f, -1f, 0f);
            }
        }
    }
}

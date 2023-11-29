using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Physics;
using MyGame.Components;
using MyGame.Resources;

namespace MyGame.Systems;

public class ResetBallSystem : ISystem
{
    private readonly IQuery<TransformComponent, BallComponent> _ballQuery;
    private readonly IQuery<PaddleComponent, TransformComponent> _paddleQuery;
    private readonly WorldSizeResource _worldSizeResource;
    private readonly ICommands _commands;
    private readonly PhysicsResource _physicsResource;

    public ResetBallSystem(
        IQuery<TransformComponent, BallComponent> ballQuery,
        WorldSizeResource worldSizeResource,
        IQuery<PaddleComponent, TransformComponent> paddleQuery,
        ICommands commands,
        PhysicsResource physicsResource)
    {
        _ballQuery = ballQuery;
        _worldSizeResource = worldSizeResource;
        _paddleQuery = paddleQuery;
        _commands = commands;
        _physicsResource = physicsResource;
    }

    public void Run(double deltaTime)
    {
        foreach (var ballComponents in _ballQuery)
        {
            var (transformComponent, _) = ballComponents;
            var localTransform = transformComponent.LocalTransform;
            if (localTransform.position.X < _worldSizeResource.Left
                || localTransform.position.X > _worldSizeResource.Right
                || localTransform.position.Y < _worldSizeResource.Bottom
                || localTransform.position.Y > _worldSizeResource.Top)
            {
                ResetBall(ballComponents.EntityId, transformComponent.LocalTransform);
            }
        }
    }

    private void ResetBall(EntityId ballId, Transform localTransform)
    {
        var paddleComponents = _paddleQuery.FirstOrDefault();
        if (paddleComponents is null)
        {
            return;
        }

        var (_, paddleTransform) = paddleComponents; 

        var result = _commands.AddChild(paddleComponents.EntityId, ballId);
        if (result.TryGetErrors(out var error))
        {
            Console.WriteLine("Could not reset ball: {0}", string.Join(", ", error));
        }

        _physicsResource.SetBody2DVelocity(ballId, Vector2.Zero);

        localTransform.position = new Vector3(0f, 0.3f, 0f);
    }
}

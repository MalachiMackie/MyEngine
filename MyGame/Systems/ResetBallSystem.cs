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
    private readonly IQuery<TransformComponent, BallComponent, KinematicBody2DComponent> _ballQuery;
    private readonly IQuery<PaddleComponent, TransformComponent> _paddleQuery;
    private readonly WorldSizeResource _worldSizeResource;
    private readonly IHierarchyCommands _hierarchyCommands;

    public ResetBallSystem(
        IQuery<TransformComponent, BallComponent, KinematicBody2DComponent> ballQuery,
        WorldSizeResource worldSizeResource,
        IQuery<PaddleComponent, TransformComponent> paddleQuery,
        IHierarchyCommands hierarchyCommands)
    {
        _ballQuery = ballQuery;
        _worldSizeResource = worldSizeResource;
        _paddleQuery = paddleQuery;
        _hierarchyCommands = hierarchyCommands;
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
                ResetBall(ballComponents.EntityId, transformComponent.LocalTransform, kinematicBody2DComponent);
            }
        }
    }

    private void ResetBall(EntityId ballId, Transform localTransform, KinematicBody2DComponent kinematicBody2DComponent)
    {
        var paddleComponents = _paddleQuery.FirstOrDefault();
        if (paddleComponents is null)
        {
            return;
        }

        var (_, paddleTransform) = paddleComponents; 

        var ballWorldScale = localTransform.scale;

        var result = _hierarchyCommands.AddChild(paddleComponents.EntityId, ballId);
        if (result.TryGetError(out var error))
        {
            Console.WriteLine("Could not reset ball: {0}", error);
        }

        kinematicBody2DComponent.Velocity = Vector2.Zero;
        localTransform.position = new Vector3(0f, 2f, 0f);
        var ballLocalScale = new Vector3(ballWorldScale.X / paddleTransform.LocalTransform.scale.X, ballWorldScale.Y / paddleTransform.LocalTransform.scale.Y, 1f);
        localTransform.scale = ballLocalScale;
    }
}

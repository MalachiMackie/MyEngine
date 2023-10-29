using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;
using MyEngine.Physics;
using MyGame.Components;

namespace MyGame.Systems;

public class LaunchBallSystem : ISystem
{
    private readonly IQuery<TransformComponent, BallComponent, DynamicBody2DComponent, ParentComponent> _playerQuery;
    private readonly InputResource _inputResource;
    private readonly PhysicsResource _physicsResource;
    private readonly ICommands _commands;

    public LaunchBallSystem(
        IQuery<TransformComponent, BallComponent, DynamicBody2DComponent, ParentComponent> playerQuery,
        InputResource inputResource,
        PhysicsResource physicsResource,
        ICommands commands)
    {
        _playerQuery = playerQuery;
        _inputResource = inputResource;
        _physicsResource = physicsResource;
        _commands = commands;
    }

    public void Run(double deltaTime)
    {
        if (_inputResource.Keyboard.IsKeyPressed(MyKey.T))
        {
            var components = _playerQuery.FirstOrDefault();
            if (components is null)
            {
                return;
            }

            var (transformComponent, ball, dynamicBody, parentComponent) = components;

            var globalScale = transformComponent.GlobalTransform.Scale;
            var globalPosition = transformComponent.GlobalTransform.Position;

            _commands.RemoveChild(parentComponent.Parent, components.EntityId);

            transformComponent.LocalTransform.scale = globalScale;
            transformComponent.LocalTransform.position = globalPosition;

            _physicsResource.ApplyImpulse(components.EntityId, new Vector3(1f, 1f, 0f) * 12f);

            ball.AttachedToPaddle = false;
        }
    }
}

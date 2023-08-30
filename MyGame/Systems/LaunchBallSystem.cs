using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyGame.Components;

namespace MyGame.Systems;

public class LaunchBallSystem : ISystem
{
    private readonly IQuery<TransformComponent, BallComponent, KinematicBody2DComponent, ParentComponent> _playerQuery;
    private readonly IHierarchyCommands _hierarchyCommands;
    private readonly InputResource _inputResource;

    public LaunchBallSystem(
        IQuery<TransformComponent, BallComponent, KinematicBody2DComponent, ParentComponent> playerQuery,
        InputResource inputResource,
        IHierarchyCommands hierarchyCommands)
    {
        _playerQuery = playerQuery;
        _inputResource = inputResource;
        _hierarchyCommands = hierarchyCommands;
    }

    public void Run(double deltaTime)
    {
        if (_inputResource.Keyboard.IsKeyPressed(MyEngine.Core.Input.MyKey.T))
        {
            var components = _playerQuery.FirstOrDefault();
            if (components is null)
            {
                return;
            }

            var (transformComponent, _, kinematicBody, parentComponent) = components;

            var globalScale = transformComponent.GlobalTransform.Scale;
            var globalPosition = transformComponent.GlobalTransform.Position;

            _hierarchyCommands.RemoveChild(parentComponent.Parent, components.EntityId);

            transformComponent.LocalTransform.scale = globalScale;
            transformComponent.LocalTransform.position = globalPosition;

            kinematicBody.Velocity += Vector2.Normalize(new Vector2(1f, 1f)) * 2f;
        }
    }
}

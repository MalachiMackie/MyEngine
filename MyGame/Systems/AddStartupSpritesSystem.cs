using MyEngine.Core;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyGame.Components;
using MyGame.Resources;
using MyGame.Utils;
using System.Numerics;

namespace MyGame.Systems;

public class AddStartupSpritesSystem : IStartupSystem
{
    private readonly IEntityCommands _entityCommands;
    private readonly ComponentContainerResource _componentContainerResource;
    private readonly ResourceRegistrationResource _resourceRegistrationResource;
    private readonly BrickSizeResource _brickSizeResource = new() { Dimensions = new Vector2(0.5f, 0.2f) };

    public AddStartupSpritesSystem(IEntityCommands entityContainerResource,
        ComponentContainerResource componentContainerResource,
        ResourceRegistrationResource resourceRegistrationResource)
    {
        _entityCommands = entityContainerResource;
        _componentContainerResource = componentContainerResource;
        _resourceRegistrationResource = resourceRegistrationResource;
    }

    public void Run()
    {
        _resourceRegistrationResource.AddResource(new WorldSizeResource {
            Bottom = -2.5f,
            Top = 2.5f,
            Left = -4.25f,
            Right = 4.25f
        });
        _resourceRegistrationResource.AddResource(_brickSizeResource);

        AddWalls();
        AddBall();
        AddBricks();
    }

    private static readonly Vector2 Origin = new(0f, 1f);

    private Vector2 GridPositionToWorldPosition(int x, int y)
    {
        return Origin + new Vector2(x * _brickSizeResource.Dimensions.X, y * _brickSizeResource.Dimensions.Y);
    }

    private void AddBricks()
    {
        var brickPositions = new[]
        {
            GridPositionToWorldPosition(0, 1),
            GridPositionToWorldPosition(1, 1),
            GridPositionToWorldPosition(2, 1),
            GridPositionToWorldPosition(-1, 1),
            GridPositionToWorldPosition(-2, 1),
            GridPositionToWorldPosition(0, 2),
            GridPositionToWorldPosition(1, 2),
            GridPositionToWorldPosition(2, 2),
            GridPositionToWorldPosition(-1, 2),
            GridPositionToWorldPosition(-2, 2),
            GridPositionToWorldPosition(0, 3),
            GridPositionToWorldPosition(1, 3),
            GridPositionToWorldPosition(2, 3),
            GridPositionToWorldPosition(-1, 3),
            GridPositionToWorldPosition(-2, 3),
        };

        foreach (var position in brickPositions)
        {
            var brick = BrickBuilder.BuildBrick(_entityCommands, position, _brickSizeResource.Dimensions.X, _brickSizeResource.Dimensions.Y);
            foreach (var component in brick.Components)
            {
                _componentContainerResource.AddComponent(brick.EntityId, component);
            }
        }
    }

    private void AddWalls()
    {
        var walls = new[]
        {
            new Transform
            {
                position = new Vector3(-4f, 0f, 0f),
                rotation = Quaternion.Identity,
                scale = new Vector3(0.1f, 4.5f, 1f)
            },
            new Transform
            {
                position = new Vector3(4f, 0f, 0f),
                rotation = Quaternion.Identity,
                scale = new Vector3(0.1f, 4.5f, 1f)
            },
            new Transform
            {
                position = new Vector3(0f, 2.25f, 0f),
                rotation = Quaternion.Identity,
                scale = new Vector3(8f, 0.1f, 1f)
            },
        };
        
        foreach (var transform in walls)
        {
            var entity = _entityCommands.AddEntity(transform);
            _componentContainerResource.AddComponent(entity, new SpriteComponent());
            _componentContainerResource.AddComponent(entity, new StaticBody2DComponent());
            _componentContainerResource.AddComponent(entity, new Collider2DComponent(new BoxCollider2D(Vector2.One)));
        }
    }

    private void AddBall()
    {
        var ballEntity = _entityCommands.AddEntity(new Transform
        {
            position = new Vector3(0f, -1f, 0f),
            rotation = Quaternion.Identity,
            scale = new Vector3(0.25f, 0.25f, 1f)
        });
        _componentContainerResource.AddComponent(ballEntity, new BallComponent());
        _componentContainerResource.AddComponent(ballEntity, new SpriteComponent());
        _componentContainerResource.AddComponent(ballEntity, new LogPositionComponent { Name = "Ball" });
        _componentContainerResource.AddComponent(ballEntity, new KinematicBody2DComponent());
        _componentContainerResource.AddComponent(ballEntity, new KinematicReboundComponent());
        _componentContainerResource.AddComponent(ballEntity, new Collider2DComponent(new CircleCollider2D(1f)));
    }
}

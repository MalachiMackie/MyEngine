using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;
using MyEngine.Physics;
using MyEngine.Utils;
using MyGame.Components;
using MyGame.Resources;
using System.Numerics;

namespace MyGame.Systems;

public class AddStartupSpritesSystem : ISystem
{
    private readonly ICommands _entityCommands;
    private readonly ResourceRegistrationResource _resourceRegistrationResource;
    private readonly GameAssets _gameAssets;
    private readonly BrickSizeResource _brickSizeResource = new() { Dimensions = new Vector2(0.5f, 0.2f) };

    private const float DefaultZIndex = 0f;

    public AddStartupSpritesSystem(ICommands entityContainerResource,
        ResourceRegistrationResource resourceRegistrationResource,
        GameAssets gameAssets)
    {
        _entityCommands = entityContainerResource;
        _resourceRegistrationResource = resourceRegistrationResource;
        _gameAssets = gameAssets;
    }

    private bool _initialized;

    public void Run(double _)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        Sprite ballSprite = _gameAssets.Ball;
        Sprite whiteSprite = _gameAssets.White;
        _resourceRegistrationResource.AddResource(new WorldSizeResource {
            Bottom = -3.5f,
            Top = 3.5f,
            Left = -4.5f,
            Right = 4.5f
        });
        _resourceRegistrationResource.AddResource(_brickSizeResource);

        if (AddWalls(whiteSprite).TryGetErrors(out var addWallsError))
        {
            Console.WriteLine("Failed to add wall: {0}", string.Join(";", addWallsError));
            return;
        }

        if (AddPaddleAndBall(whiteSprite, ballSprite).IsFailure)
        {
            return;
        }

        if (AddBricks(whiteSprite).TryGetErrors(out var addBricksError))
        {
            Console.WriteLine("Failed to add brick: {0}", string.Join(';', addBricksError));
        }
    }

    private static readonly Vector2 Origin = new(0f, 1f);

    private Vector2 GridPositionToWorldPosition(int x, int y)
    {
        return Origin + new Vector2(x * _brickSizeResource.Dimensions.X, y * _brickSizeResource.Dimensions.Y);
    }

    private Result<Unit> AddBricks(Sprite whiteSprite)
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

        var successfulBricks = new List<EntityId>(brickPositions.Length);

        foreach (var position in brickPositions)
        {
            var createEntityResult = _entityCommands.CreateEntity(Transform.Default(position: position.Extend(DefaultZIndex), scale: _brickSizeResource.Dimensions.Extend(1f)),
                new SpriteComponent(whiteSprite),
                new StaticBody2DComponent(),
                new Collider2DComponent(new BoxCollider2D(Vector2.One)),
                new BrickComponent());

            if (createEntityResult.TryGetValue(out var brickId))
            {
                successfulBricks.Add(brickId);
            }
            else
            {
                foreach (var brick in successfulBricks)
                {
                    _entityCommands.RemoveEntity(brick).Expect(because: "We checked that entity was added successfully");
                }

                return createEntityResult.Map(_ => Unit.Value);
            }
        }

        return Result.Success<Unit>(Unit.Value);
    }

    private Result<Unit> AddWalls(Sprite whiteSprite)
    {
        // 8 x 6
        var walls = new[]
        {
            new Transform
            {
                position = new Vector3(-3.95f, 0f, DefaultZIndex),
                rotation = Quaternion.Identity,
                scale = new Vector3(0.1f, 6f, 1f)
            },
            new Transform
            {
                position = new Vector3(3.95f, 0f, DefaultZIndex),
                rotation = Quaternion.Identity,
                scale = new Vector3(0.1f, 6f, 1f)
            },
            new Transform
            {
                position = new Vector3(0f, 2.95f, DefaultZIndex),
                rotation = Quaternion.Identity,
                scale = new Vector3(8f, 0.1f, 1f)
            },
        };

        var successfulWalls = new List<EntityId>(walls.Length);
        
        foreach (var transform in walls)
        {
            var addWallResult = _entityCommands.CreateEntity(transform,
                new SpriteComponent(whiteSprite),
                new StaticBody2DComponent(),
                new Collider2DComponent(new BoxCollider2D(Vector2.One)));

            if (addWallResult.IsFailure)
            {
                foreach (var wall in successfulWalls)
                {
                    _entityCommands.RemoveEntity(wall).Expect("We checked success of add wall");
                }

                return Result.Failure<Unit, EntityId>(addWallResult);
            }
        }

        return Result.Success<Unit>(Unit.Value);
    }


    private Result<Unit> AddPaddleAndBall(Sprite whiteSprite, Sprite ballSprite)
    {
        var result = _entityCommands.CreateEntity(x => x
            .WithDefaultTransform(new Vector3(0f, -1.25f, DefaultZIndex))
            .WithComponents(
                new SpriteComponent(whiteSprite, dimensions: new Vector2(1.5f, 0.15f)),
                new KinematicBody2DComponent(),
                new Collider2DComponent(new BoxCollider2D(new Vector2(1.5f, 0.15f))),
                new PaddleComponent())
            .WithChild(x => x
                .WithDefaultTransform(new Vector3(0f, 0.3f, DefaultZIndex))
                .WithComponents(
                    new SpriteComponent(ballSprite),
                    new DynamicBody2DComponent(),
                    new BouncinessComponent(1f),
                    new Collider2DComponent(new CircleCollider2D(radius: 0.125f)),
                    new BallComponent { AttachedToPaddle = true},
                    new VelocityComponent(),
                    new LogPositionComponent { Name = "Ball"})));

        if (result.TryGetErrors(out var errors))
        {
            Console.WriteLine("Failed to create paddle and ball: {0}", string.Join(";", errors));
        }

        return result;
    }
}


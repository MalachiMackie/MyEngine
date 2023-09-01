using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Physics;
using MyEngine.Rendering;
using MyEngine.Utils;
using MyGame.Components;
using MyGame.Resources;
using StbImageSharp;
using System.Numerics;

using AddPaddleAndBallError = MyEngine.Utils.OneOf<
    MyGame.Systems.AddStartupSpritesSystem.AddPaddleError,
    MyGame.Systems.AddStartupSpritesSystem.AddBallError,
    MyGame.Systems.AddStartupSpritesSystem.AddBallAsPaddleChildError>;

namespace MyGame.Systems;

public class AddStartupSpritesSystem : IStartupSystem
{
    private readonly ICommands _entityCommands;
    private readonly ResourceRegistrationResource _resourceRegistrationResource;
    private readonly IHierarchyCommands _hierarchyCommands; 
    private readonly BrickSizeResource _brickSizeResource = new() { Dimensions = new Vector2(0.5f, 0.2f) };
    private readonly Sprite _silkLogoSprite;
    private readonly Sprite _whiteSprite;

    public AddStartupSpritesSystem(ICommands entityContainerResource,
        ResourceRegistrationResource resourceRegistrationResource,
        IHierarchyCommands hierarchyCommands)
    {
        _entityCommands = entityContainerResource;
        _resourceRegistrationResource = resourceRegistrationResource;
        _hierarchyCommands = hierarchyCommands;

        using var image = File.OpenRead("silk.png");
        var imageResult = ImageResult.FromStream(image, ColorComponents.RedGreenBlueAlpha);
        _silkLogoSprite = new Sprite(new SpriteId(Guid.NewGuid()), new Vector2(imageResult.Width, imageResult.Height), 100, imageResult.Data);

        using var whiteImage = File.OpenRead("White.png");
        var whiteImageResult = ImageResult.FromStream(whiteImage, ColorComponents.RedGreenBlueAlpha);
        _whiteSprite = new Sprite(new SpriteId(Guid.NewGuid()), new Vector2(whiteImageResult.Width, whiteImageResult.Height), 100, whiteImageResult.Data);
    }

    public void Run()
    {
        _resourceRegistrationResource.AddResource(new WorldSizeResource {
            Bottom = -3.5f,
            Top = 3.5f,
            Left = -4.5f,
            Right = 4.5f
        });
        _resourceRegistrationResource.AddResource(_brickSizeResource);

        if (AddWalls().TryGetError(out var addWallsError))
        {
            Console.WriteLine("Failed to add wall: {0}", addWallsError);
            return;
        }

        if (AddPaddleAndBall().TryGetError(out var addPaddleAndBallError))
        {
            addPaddleAndBallError.Match(
                addPaddleError => Console.WriteLine("Failed to add paddle: {0}", addPaddleError.Error),
                addBallError => Console.WriteLine("Failed to add ball: {0}", addBallError.Error),
                addBallAsPaddleChildError => Console.WriteLine("Failed to add ball as paddle child: {0}", addBallAsPaddleChildError.Error));
            return;
        }

        if (AddBricks().TryGetError(out var addBricksError))
        {
            Console.WriteLine("Failed to add brick: {0}", addBricksError);
        }
    }

    private static readonly Vector2 Origin = new(0f, 1f);

    private Vector2 GridPositionToWorldPosition(int x, int y)
    {
        return Origin + new Vector2(x * _brickSizeResource.Dimensions.X, y * _brickSizeResource.Dimensions.Y);
    }

    private Result<Unit, AddEntityCommandError> AddBricks()
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
            var createEntityResult = _entityCommands.CreateEntity(Transform.Default(position: position.Extend(3.0f), scale: _brickSizeResource.Dimensions.Extend(1f)),
                new SpriteComponent(_whiteSprite),
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

        return Result.Success<Unit, AddEntityCommandError>(Unit.Value);
    }

    private Result<Unit, AddEntityCommandError> AddWalls()
    {
        // 8 x 6
        var walls = new[]
        {
            new Transform
            {
                position = new Vector3(-3.95f, 0f, 0f),
                rotation = Quaternion.Identity,
                scale = new Vector3(0.1f, 6f, 1f)
            },
            new Transform
            {
                position = new Vector3(3.95f, 0f, 0f),
                rotation = Quaternion.Identity,
                scale = new Vector3(0.1f, 6f, 1f)
            },
            new Transform
            {
                position = new Vector3(0f, 2.95f, 0f),
                rotation = Quaternion.Identity,
                scale = new Vector3(8f, 0.1f, 1f)
            },
        };

        var successfulWalls = new List<EntityId>(walls.Length);
        
        foreach (var transform in walls)
        {
            var addWallResult = _entityCommands.CreateEntity(transform,
                new SpriteComponent(_whiteSprite),
                new StaticBody2DComponent(),
                new Collider2DComponent(new BoxCollider2D(Vector2.One)));

            if (addWallResult.IsFailure)
            {
                foreach (var wall in successfulWalls)
                {
                    _entityCommands.RemoveEntity(wall).Expect("We checked success of add wall");
                }

                return addWallResult.Map(_ => Unit.Value);
            }
        }

        return Result.Success<Unit, AddEntityCommandError>(Unit.Value);
    }


    private Result<Unit, AddPaddleAndBallError> AddPaddleAndBall()
    {
        var paddleScale = new Vector3(1.5f, 0.15f, 1f);
        var paddleIdResult = _entityCommands.CreateEntity(new Transform
            {
                position = new Vector3(0f, -1.25f, 0f),
                rotation = Quaternion.Identity,
                scale = paddleScale
            },
            new SpriteComponent(_whiteSprite),
            new KinematicBody2DComponent(),
            new Collider2DComponent(new BoxCollider2D(Vector2.One)),
            new PaddleComponent())
            .MapError(x => new AddPaddleError(x));

        if (!paddleIdResult.TryGetValue(out var paddleId))
        {
            return Result.Failure<Unit, AddPaddleAndBallError>(new AddPaddleAndBallError(paddleIdResult.UnwrapError()));
        }

        Console.WriteLine("PaddleId: {0}", paddleId);

        var worldBallScale = new Vector3(0.25f, 0.25f, 1f);

        var ballScale = new Vector3(worldBallScale.X / paddleScale.X, worldBallScale.Y / paddleScale.Y, worldBallScale.Z / paddleScale.Z);

        var ballIdResult = _entityCommands.CreateEntity(new Transform
            {
                position = new Vector3(0f, 2f, 0f),
                rotation = Quaternion.Identity,
                scale = ballScale
            },
            new SpriteComponent(_silkLogoSprite),
            new KinematicBody2DComponent(),
            new Collider2DComponent(new CircleCollider2D(worldBallScale.X * 0.5f)),
            new BallComponent(),
            new LogPositionComponent { Name = "Ball" },
            new KinematicReboundComponent())
            .MapError(x => new AddBallError(x));

        if (!ballIdResult.TryGetValue(out var ballId))
        {
            _entityCommands.RemoveEntity(paddleId).Expect("We checked the success of add paddle");
            return Result.Failure<Unit, AddPaddleAndBallError>(new AddPaddleAndBallError(ballIdResult.UnwrapError()));
        }

        Console.WriteLine("BallId: {0}", ballId);

        var addChildResult = _hierarchyCommands.AddChild(paddleId, ballId);
        if (addChildResult.TryGetError(out var error))
        {
            _entityCommands.RemoveEntity(paddleId).Expect("We checked the success of add paddle");
            _entityCommands.RemoveEntity(ballId).Expect("We checked the success of add ball");

            return Result.Failure<Unit, AddPaddleAndBallError>(new AddPaddleAndBallError(new AddBallAsPaddleChildError(error)));
        }

        return Result.Success<Unit, AddPaddleAndBallError>(Unit.Value);
    }

    public readonly record struct AddPaddleError(AddEntityCommandError Error);
    public readonly record struct AddBallError(AddEntityCommandError Error);
    public readonly record struct AddBallAsPaddleChildError(AddChildError Error);
}


using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Physics;
using MyEngine.Rendering;
using MyEngine.Utils;
using MyGame.Components;
using MyGame.Resources;
using System.Diagnostics;
using System.Numerics;

using AddPaddleAndBallError = MyEngine.Utils.OneOf<
    MyGame.Systems.AddStartupSpritesSystem.AddPaddleError,
    MyGame.Systems.AddStartupSpritesSystem.AddBallError,
    MyGame.Systems.AddStartupSpritesSystem.AddBallAsPaddleChildError>;

namespace MyGame.Systems;

public class AddStartupSpritesSystem : ISystem
{
    private readonly ICommands _entityCommands;
    private readonly ResourceRegistrationResource _resourceRegistrationResource;
    private readonly IHierarchyCommands _hierarchyCommands;
    private readonly SpriteAssetIdsResource _spriteAssetIds;
    private readonly BrickSizeResource _brickSizeResource = new() { Dimensions = new Vector2(0.5f, 0.2f) };
    private readonly AssetCollection _assetCollection;

    public AddStartupSpritesSystem(ICommands entityContainerResource,
        ResourceRegistrationResource resourceRegistrationResource,
        IHierarchyCommands hierarchyCommands,
        SpriteAssetIdsResource spriteAssetIds,
        AssetCollection assetCollection)
    {
        _entityCommands = entityContainerResource;
        _resourceRegistrationResource = resourceRegistrationResource;
        _hierarchyCommands = hierarchyCommands;
        _spriteAssetIds = spriteAssetIds;
        _assetCollection = assetCollection;
    }

    private bool _loadingFailed = false;
    private bool _loadingSucceeded = false;

    public void Run(double _)
    {
        if (_loadingFailed || _loadingSucceeded)
        {
            return;
        }

        var tuple = (_assetCollection.TryGetAsset<Sprite>(_spriteAssetIds.SilkSpriteId),
            _assetCollection.TryGetAsset<Sprite>(_spriteAssetIds.WhiteSpriteId));

        Sprite silkSprite;
        Sprite whiteSprite;

        switch (tuple)
        {
            case ({ IsSuccess: true } silkSpriteResult, { IsSuccess: true } whiteSpriteResult):
                {
                    _loadingSucceeded = true;
                    silkSprite = silkSpriteResult.Unwrap();
                    whiteSprite = whiteSpriteResult.Unwrap();
                    break;
                }
            case ({ IsFailure: true } failure, { IsSuccess: true }):
                {
                    var err = failure.UnwrapError();
                    if (err == AssetCollection.GetAssetError.IncorrectAssetType)
                    {
                        Console.WriteLine("Failed to load asset");
                        _loadingFailed = true;
                    }
                    return;
                }
            case ({ IsSuccess: true }, { IsFailure: true } failure):
                {
                    var err = failure.UnwrapError();
                    if (err == AssetCollection.GetAssetError.IncorrectAssetType)
                    {
                        Console.WriteLine("Failed to load asset");
                        _loadingFailed = true;
                    }
                    return;
                }
            // both failed
            case ({ IsFailure: true } failureA, { IsFailure: true } failureB):
                {
                    var errA = failureA.UnwrapError();
                    var errB = failureB.UnwrapError();
                    if (errA == AssetCollection.GetAssetError.IncorrectAssetType
                        || errB == AssetCollection.GetAssetError.IncorrectAssetType)
                    {
                        Console.WriteLine("Failed to load asset");
                        _loadingFailed = true;
                    }
                    return;
                }
            default:
                {
                    throw new UnreachableException();
                }
        }

        _resourceRegistrationResource.AddResource(new WorldSizeResource {
            Bottom = -3.5f,
            Top = 3.5f,
            Left = -4.5f,
            Right = 4.5f
        });
        _resourceRegistrationResource.AddResource(_brickSizeResource);

        if (AddWalls(whiteSprite).TryGetError(out var addWallsError))
        {
            Console.WriteLine("Failed to add wall: {0}", addWallsError);
            return;
        }

        if (AddPaddleAndBall(whiteSprite, silkSprite).TryGetError(out var addPaddleAndBallError))
        {
            addPaddleAndBallError.Match(
                addPaddleError => Console.WriteLine("Failed to add paddle: {0}", addPaddleError.Error),
                addBallError => Console.WriteLine("Failed to add ball: {0}", addBallError.Error),
                addBallAsPaddleChildError => Console.WriteLine("Failed to add ball as paddle child: {0}", addBallAsPaddleChildError.Error));
            return;
        }

        if (AddBricks(whiteSprite).TryGetError(out var addBricksError))
        {
            Console.WriteLine("Failed to add brick: {0}", addBricksError);
        }
    }

    private static readonly Vector2 Origin = new(0f, 1f);

    private Vector2 GridPositionToWorldPosition(int x, int y)
    {
        return Origin + new Vector2(x * _brickSizeResource.Dimensions.X, y * _brickSizeResource.Dimensions.Y);
    }

    private Result<Unit, AddEntityCommandError> AddBricks(Sprite whiteSprite)
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

        return Result.Success<Unit, AddEntityCommandError>(Unit.Value);
    }

    private Result<Unit, AddEntityCommandError> AddWalls(Sprite whiteSprite)
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
                new SpriteComponent(whiteSprite),
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


    private Result<Unit, AddPaddleAndBallError> AddPaddleAndBall(Sprite whiteSprite, Sprite silkLogoSprite)
    {
        var paddleScale = new Vector3(1.5f, 0.15f, 1f);
        var paddleIdResult = _entityCommands.CreateEntity(new Transform
            {
                position = new Vector3(0f, -1.25f, 0f),
                rotation = Quaternion.Identity,
                scale = paddleScale
            },
            new SpriteComponent(whiteSprite),
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
            new SpriteComponent(silkLogoSprite),
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


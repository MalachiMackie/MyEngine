using System.Numerics;
using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;
using MyEngine.Physics;
using MyEngine.Rendering;
using MyEngine.Utils;

namespace MyEngine.TestScenarios.Physics.Bouncing.DynamicCollisions;

internal class DynamicToDynamicBouncingScenario
{
    public static void Register(AppBuilder appBuilder)
    {
        appBuilder.AddStartupSystem<SetupSystem>()
            .AddSystem<LaunchBallsSystem>(ISystemStage.Update)
            .AddResource(new NextPairResource());
    }
}

file static class ColliderPairs
{
    public static ColliderPair[] Pairs = new[] {
        // --- One Moving, One Stationary

        // vertical collisions
        new ColliderPair(
            new Vector3(0f, -1f, 0f), new Vector2(0f, 1.5f),
            new Vector3(0f, 1f, 0f), new Vector2(0f, 0f),
            1f),
        new ColliderPair(
            new Vector3(0f, -1f, 0f), new Vector2(0f, 0f),
            new Vector3(0f, 1f, 0f), new Vector2(0f, -1.5f),
            1f),

        // horizontal collisions
        new ColliderPair(
            new Vector3(-1f, 0f, 0f), new Vector2(1.5f, 0f),
            new Vector3(0f, 0f, 0f), new Vector2(0f, 0f),
            1f),
        new ColliderPair(
            new Vector3(-1f, 0f, 0f), new Vector2(0f, 0f),
            new Vector3(0f, 0f, 0f), new Vector2(-1.5f, 0f),
            1f),

        // diagonal collisions
        new ColliderPair(
            new Vector3(-1f, -1f, 0f), new Vector2(1f, 1f).WithMagnitude(1.5f).Unwrap(),
            new Vector3(0f, 0f, 0f), new Vector2(0f, 0f),
            1f),

        // --- Both Moving

        // vertical collisions
        new ColliderPair(
            new Vector3(0f, -1f, 0f), new Vector2(0f, 1f).WithMagnitude(0.75f).Unwrap(),
            new Vector3(0f, 1f, 0f), new Vector2(0f, -1f).WithMagnitude(0.75f).Unwrap(),
            1f),
        new ColliderPair(
            new Vector3(0f, -1f, 0f), new Vector2(0f, 1f).WithMagnitude(0.5f).Unwrap(),
            new Vector3(0f, 1f, 0f), new Vector2(0f, -1f).WithMagnitude(1f).Unwrap(),
            1f),

        // horizontal collisions
        new ColliderPair(
            new Vector3(1f, 0f, 0f), new Vector2(-1f, 0f).WithMagnitude(0.75f).Unwrap(),
            new Vector3(0f, 0f, 0f), new Vector2(1f, 0f).WithMagnitude(0.75f).Unwrap(),
            1f),
        new ColliderPair(
            new Vector3(1f, 0f, 0f), new Vector2(-1f, 0f).WithMagnitude(1f).Unwrap(),
            new Vector3(0f, 0f, 0f), new Vector2(1f, 0f).WithMagnitude(0.5f).Unwrap(),
            1f),

        // diagonal collisions
        new ColliderPair(
            new Vector3(-1f, 0f, 0f), new Vector2(1f, 1f).WithMagnitude(1.5f).Unwrap(),
            new Vector3(1f, 0f, 0f), new Vector2(-1f, 1f).WithMagnitude(1.5f).Unwrap(),
            1f),

        new ColliderPair(
            new Vector3(-0.5f, 0f, 0f), new Vector2(0.5f, 1f).WithMagnitude(1.5f).Unwrap(),
            new Vector3(1f, 0f, 0f), new Vector2(-1f, 1f).WithMagnitude(1.5f).Unwrap(),
            1f), // todo: this is broken

        new ColliderPair(
            new Vector3(-0.5f, 0f, 0f), new Vector2(0.5f, 1f),
            new Vector3(1f, 0f, 0f), new Vector2(-1f, 1f),
            1f), // but this isnt
    };
}


public class NextPairResource : IResource
{
    public int NextPair { get; set; }
}

public class SetupSystem : IStartupSystem
{

    private readonly ICommands _commands;

    public SetupSystem(ICommands commands)
    {
        _commands = commands;
    }

    public void Run()
    {
        var whiteTexture = Texture.Create(AssetId.Generate(), new TextureCreateData(1, new byte[] { 255, 255, 255, 255 }, 1, 1));
        var whiteSprite = Sprite.Create(AssetId.Generate(), new CreateSpriteFromTextureAtlas(whiteTexture, Vector2.Zero, Vector2.One, SpriteOrigin.Center));
        using var file = File.OpenRead("ball.png");
        var ball = Sprite.LoadAsync(AssetId.Generate(), file, new CreateSpriteFromFullTexture(100, SpriteOrigin.Center)).GetAwaiter().GetResult();

        _commands.CreateEntity(x => x.WithDefaultTransform(position: new Vector3())
            .WithComponents(new Camera2DComponent(new Vector2(8f, 6f))))
            .Unwrap();

        _commands.CreateEntity(x => x.WithDefaultTransform(position: new Vector3(-1, 0, 0))
            .WithComponents(
            new SpriteComponent(ball),
            new Collider2DComponent(new CircleCollider2D(0.25f * 0.5f)),
            new DynamicBody2DComponent(),
            new BouncinessComponent(1f),
            new BallComponent()));

        _commands.CreateEntity(x => x.WithDefaultTransform(position: new Vector3(1, 0, 0))
            .WithComponents(
            new SpriteComponent(ball),
            new Collider2DComponent(new CircleCollider2D(0.25f * 0.5f)),
            new DynamicBody2DComponent(),
            new BouncinessComponent(1f),
            new BallComponent()));
    }
}

public record ColliderPair(Vector3 PositionA, Vector2 VelocityA, Vector3 PositionB, Vector2 VelocityB, float Bounciness);

public class BallComponent : IComponent
{
}


public class LaunchBallsSystem : ISystem
{
    private readonly IQuery<TransformComponent, BouncinessComponent, BallComponent> _balls;
    private readonly PhysicsResource _physicsResource;
    private readonly IKeyboard _keyboard;
    private readonly NextPairResource _nextPairResource;

    public LaunchBallsSystem(
        IQuery<TransformComponent, BouncinessComponent, BallComponent> balls,
        PhysicsResource physicsResource,
        IKeyboard keyboard,
        NextPairResource nextPairResource)
    {
        _balls = balls;
        _physicsResource = physicsResource;
        _keyboard = keyboard;
        _nextPairResource = nextPairResource;
    }

    private const float _velocityScale = 1.5f;

    public void Run(double deltaTime)
    {
        var balls = _balls.ToArray();
        if (balls.Length != 2)
        {
            throw new InvalidOperationException("There should be 2 balls");
        }

        if (_keyboard.IsKeyPressed(MyKey.Space))
        {
            var (positionA, velocityA, positionB, velocityB, bounciness) = ColliderPairs.Pairs[_nextPairResource.NextPair];
            var (transformA, bouncinessA, _) = balls[0];
            var (transformB, bouncinessB, _) = balls[1];
            
            transformA.LocalTransform.position = positionA;
            transformB.LocalTransform.position = positionB;
            bouncinessA.Bounciness = bounciness;
            bouncinessB.Bounciness = bounciness;
            _physicsResource.SetBody2DVelocity(balls[0].EntityId, velocityA);
            _physicsResource.SetBody2DVelocity(balls[1].EntityId, velocityB);

            _nextPairResource.NextPair++;
            if (_nextPairResource.NextPair >= ColliderPairs.Pairs.Length)
            {
                _nextPairResource.NextPair = 0;
            }
        }
    }
}


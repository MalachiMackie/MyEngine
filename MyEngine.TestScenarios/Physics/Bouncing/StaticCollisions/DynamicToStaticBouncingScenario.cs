using System.Numerics;
using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Rendering;
using MyEngine.Input;
using MyEngine.Physics;
using MyEngine.Rendering;
using MyEngine.UI;

namespace MyEngine.TestScenarios.Physics.Bouncing.StaticCollisions;

internal class DynamicToStaticBouncingScenario
{
    public static void Register(AppBuilder appBuilder)
    {
        appBuilder.AddStartupSystem<SetupSystem>()
            .AddResource(new NextTestResource())
            .AddSystem<LaunchBallsSystem>(ISystemStage.Update)
            .AddSystem<VelocityDisplaySystem>(ISystemStage.Update);
    }
}

file static class BallProperties
{
    public static BallProps[] Balls = new[] {
        // straight bouncing
        new BallProps(new Vector3(0, 0, 0), new Vector2(0f, 1f), 1f),
        new BallProps(new Vector3(0, 0, 0), new Vector2(0f, 1f), 0.75f),
        new BallProps(new Vector3(0, 0, 0), new Vector2(0f, 1f), 0.5f),
        new BallProps(new Vector3(0, 0, 0), new Vector2(0f, 1f), 0.25f),
        new BallProps(new Vector3(0, 0, 0), new Vector2(0f, 1f), 0f),
        // diagonal bouncing
        new BallProps(new Vector3(-1f, 0, 0), new Vector2(1f, 1f), 1f),
        new BallProps(new Vector3(-1f, 0, 0), new Vector2(1f, 1f), 0.75f),
        new BallProps(new Vector3(-1f, 0, 0), new Vector2(1f, 1f), 0.5f),
        new BallProps(new Vector3(-1f, 0, 0), new Vector2(1f, 1f), 0.25f),
        new BallProps(new Vector3(-1f, 0, 0), new Vector2(1f, 1f), 0f),


        // currently a bounce of 0 still results in a little bit of perceived bounce.
        // That's because the bounce system doesn't separate the colliders,
        // and so the physics system does that, but it ends up with a little bit of 'exit' velocity
    };
}


public class NextTestResource : IResource
{
    public int NextTest { get; set; }
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
        using var fontImage = File.OpenRead("Hermit-Regular-fed68123.png");
        var font = FontAsset.LoadAsync(AssetId.Generate(), fontImage).GetAwaiter().GetResult();

        _commands.CreateEntity(x => x.WithDefaultTransform(position: new Vector3())
            .WithComponents(new Camera2DComponent(new Vector2(8f, 6f))))
            .Unwrap();

        _commands.CreateEntity(x => x.WithDefaultTransform(position: new Vector3(0f, 2f, 0f))
            .WithComponents(
                new StaticBody2DComponent(),
                new Collider2DComponent(new BoxCollider2D(new Vector2(10f, 0.3f))),
                new SpriteComponent(Sprite.Create(AssetId.Generate(), new CreateSpriteFromTextureAtlas(
                    whiteTexture,
                    Vector2.Zero, Vector2.One,
                    SpriteOrigin.Center)), new Vector2(6f, 0.3f))))
            .Unwrap();

        _commands.CreateEntity(x => x.WithDefaultTransform(position: new Vector3(0, 0, 0))
            .WithComponents(
            new SpriteComponent(ball),
            new Collider2DComponent(new CircleCollider2D(0.25f * 0.5f)),
            new DynamicBody2DComponent(),
            new BouncinessComponent(1f),
            new BallComponent(),
            new VelocityComponent()));

        _commands.CreateEntity(x => x.WithDefaultTransform()
            .WithComponents(new UICanvasComponent())
            .WithChild(x => x.WithDefaultTransform()
                .WithComponents(
                    new UITextComponent() { Font = font, Text = "" },
                    new UITransformComponent() { Position = new Vector3(10f, 550f, 0f)})));
    }
}

public record BallProps(Vector3 PositionA, Vector2 VelocityA, float Bounciness);

public class BallComponent : IComponent
{
}

public class VelocityDisplaySystem : ISystem
{
    private readonly IQuery<VelocityComponent, BallComponent> _ballQuery;
    private readonly IQuery<UITextComponent> _textQuery;

    public VelocityDisplaySystem(IQuery<UITextComponent> textQuery, IQuery<VelocityComponent, BallComponent> ballQuery)
    {
        _textQuery = textQuery;
        _ballQuery = ballQuery;
    }

    public void Run(double deltaTime)
    {
        var ball = _ballQuery.FirstOrDefault();
        var text = _textQuery.FirstOrDefault();

        if (ball is null || text is null)
        {
            return;
        }

        var velocity = ball.Component1.Velocity;
        text.Component1.Text = $"{velocity.X}, {velocity.Y}";
    }
}


public class LaunchBallsSystem : ISystem
{
    private readonly IQuery<TransformComponent, BouncinessComponent, BallComponent> _ball;
    private readonly PhysicsResource _physicsResource;
    private readonly InputResource _inputResource;
    private readonly NextTestResource _nextTestResource;

    public LaunchBallsSystem(
        IQuery<TransformComponent, BouncinessComponent, BallComponent> ball,
        PhysicsResource physicsResource,
        InputResource inputResource,
        NextTestResource nextTestResource)
    {
        _ball = ball;
        _physicsResource = physicsResource;
        _inputResource = inputResource;
        _nextTestResource = nextTestResource;
    }

    private const float _velocityScale = 3f;

    public void Run(double deltaTime)
    {
        var balls = _ball.ToArray();
        if (balls.Length != 1)
        {
            throw new InvalidOperationException("There should be 2 balls");
        }

        if (_inputResource.Keyboard.IsKeyPressed(MyKey.Space))
        {
            var (position, velocity, bounciness) = BallProperties.Balls[_nextTestResource.NextTest];
            var (transform, bouncinessComponent, _) = balls[0];

            transform.LocalTransform.position = position;
            bouncinessComponent.Bounciness = bounciness;
            _physicsResource.SetBody2DVelocity(balls[0].EntityId, velocity * _velocityScale);

            _nextTestResource.NextTest++;
            if (_nextTestResource.NextTest >= BallProperties.Balls.Length)
            {
                _nextTestResource.NextTest = 0;
            }
        }
    }
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Utils;
using Xunit.Abstractions;

namespace MyEngine.Rendering.Tests;
public class RenderSystemTests
{
    private readonly RenderSystem _system;
    private readonly RenderStats _renderStats = new();
    private readonly IRenderer _renderer = A.Fake<IRenderer>();
    private readonly IQuery<Camera2DComponent, TransformComponent> _camera2DQuery = A.Fake<IQuery<Camera2DComponent, TransformComponent>>();
    private readonly IRenderCommandQueue _renderCommandQueue = A.Fake<IRenderCommandQueue>();

    private static readonly Sprite WorldSprite = A.Dummy<Sprite>();
    private static readonly Sprite ScreenSprite = A.Dummy<Sprite>();
    private static readonly Vector2 WorldSpriteDimensions = new();
    private static readonly GlobalTransform WorldSpriteTransform = GlobalTransform.FromTransform(Transform.Default(new Vector3(0, 1, 2)));
    private static readonly float WorldSpriteTransparency = 0.8f;
    private static readonly float ScreenSpriteTransparency = 0.7f;
    private static readonly Vector3 ScreenSpritePosition = new(1f, 2f, 3f);
    private static readonly Vector2 ScreenSpriteDimensions = new(1f, 1.1f);
    private static readonly Vector3 LineStart = new(0f, 0f, 0f);
    private static readonly Vector3 LineEnd = new(1f, 1f, 1f);
    private static readonly Texture TextTexture = A.Dummy<Texture>();
    private static readonly Dictionary<char, Sprite> TextCharacterSprites = new() { { 'a', A.Dummy<Sprite>() } };
    private static readonly string Text = "Hello World";
    private static readonly float TextTransparency = 0.5f;
    private static readonly Vector3 TextPosition = new(5f, 6f, 7f);
    private static readonly Transform CameraTransform = new();
    private static readonly Vector2 CameraSize = new();

    public RenderSystemTests()
    {
        _system = new RenderSystem(
            _renderer,
            A.Fake<IQuery<Camera3DComponent, TransformComponent>>(),
            _camera2DQuery,
            _renderCommandQueue,
            _renderStats);

        A.CallTo(() => _renderCommandQueue.Flush())
            .Returns(new IRenderCommand[] {
                new RenderSpriteCommand(WorldSprite, WorldSpriteDimensions, WorldSpriteTransform, WorldSpriteTransparency),
                new RenderScreenSpaceSpriteCommand(ScreenSprite, ScreenSpriteTransparency, ScreenSpritePosition, ScreenSpriteDimensions),
                new RenderLineCommand(LineStart, LineEnd),
                new RenderScreenSpaceTextCommand(TextTexture, TextCharacterSprites, Text, TextTransparency, TextPosition)
            });


        A.CallTo(() => _camera2DQuery.GetEnumerator())
            .Returns(new[] {
                new EntityComponents<Camera2DComponent, TransformComponent>(EntityId.Generate())
                {
                    Component1 = new Camera2DComponent(CameraSize),
                    Component2 = new TransformComponent(CameraTransform)
                }
            }.AsEnumerable().GetEnumerator());
        
    }

    [Fact]
    public void Should_Render2DObjects_When_Camera2DIsFound()
    {
        _system.Run(0);

        A.CallTo(() => _renderer.RenderOrthographic(
            CameraTransform.position,
            CameraSize,
            An<IEnumerable<IRenderer.SpriteRender>>.That.IsEquivalentTo(new[] {
                new IRenderer.SpriteRender(){
                    Sprite = WorldSprite,
                    Dimensions = WorldSpriteDimensions,
                    Transparency = WorldSpriteTransparency,
                    ModelMatrix = WorldSpriteTransform.ModelMatrix
                },
            }),
            An<IEnumerable<IRenderer.SpriteRender>>.That.IsEquivalentTo(new[]
            {
                new IRenderer.SpriteRender(){
                    Sprite = ScreenSprite,
                    Dimensions = ScreenSprite.Dimensions,
                    Transparency = ScreenSpriteTransparency,
                    ModelMatrix = Matrix4x4.CreateScale(ScreenSpriteDimensions.Extend(1f)) * Matrix4x4.CreateTranslation(ScreenSpritePosition)
                },
            }),
            An<IEnumerable<IRenderer.LineRender>>.That.IsEquivalentTo(new[]
            {
                new IRenderer.LineRender()
                {
                    Start = LineStart,
                    End = LineEnd
                }
            }),
            An<IEnumerable<IRenderer.TextRender>>.That.IsEquivalentTo(new[]
            {
                new IRenderer.TextRender(TextPosition, Text, TextTransparency, TextTexture, 30, 20, TextCharacterSprites)
            }))).MustHaveHappened();
    }

    [Fact]
    public void Should_AssignRenderStatsDrawCalls()
    {
        A.CallTo(() => _renderer.RenderOrthographic(
            A<Vector3>._,
            A<Vector2>._,
            An<IEnumerable<IRenderer.SpriteRender>>._,
            An<IEnumerable<IRenderer.SpriteRender>>._,
            An<IEnumerable<IRenderer.LineRender>>._,
            An<IEnumerable<IRenderer.TextRender>>._))
            .Returns(new IRenderer.RendererStats(15));

        _system.Run(1);

        _renderStats.DrawCalls.Should().Be(15);
    }

    [Fact]
    public void Should_NotRenderAnythingWhenNoCameraIsFound()
    {
        A.CallTo(() => _camera2DQuery.GetEnumerator())
            .Returns(Array.Empty<EntityComponents<Camera2DComponent, TransformComponent>>().AsEnumerable().GetEnumerator());

        _system.Run(1);

        A.CallTo(() => _renderer.RenderOrthographic(
            A<Vector3>._,
            A<Vector2>._,
            An<IEnumerable<IRenderer.SpriteRender>>._,
            An<IEnumerable<IRenderer.SpriteRender>>._,
            An<IEnumerable<IRenderer.LineRender>>._,
            An<IEnumerable<IRenderer.TextRender>>._))
            .MustNotHaveHappened();
    }
}

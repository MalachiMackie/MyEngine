using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Utils;

namespace MyEngine.Rendering;

public class RenderSystem : ISystem
{
    private readonly IRenderer _renderer;
    private readonly IRenderCommandQueue _renderCommandQueue;
    private readonly IQuery<Camera3DComponent, TransformComponent> _camera3DQuery;
    private readonly IQuery<Camera2DComponent, TransformComponent> _camera2DQuery;
    private readonly RenderStats _renderStats;

    public RenderSystem(
        IRenderer renderer,
        IQuery<Camera3DComponent, TransformComponent> camera3DQuery,
        IQuery<Camera2DComponent, TransformComponent> camera2DQuery,
        IRenderCommandQueue renderCommandQueue,
        RenderStats renderStats)
    {
        _renderer = renderer;
        _camera3DQuery = camera3DQuery;
        _camera2DQuery = camera2DQuery;
        _renderCommandQueue = renderCommandQueue;
        _renderStats = renderStats;
    }

    public void Run(double deltaTime)
    {
        if (!TryRender2D())
        {
            Console.WriteLine("No Camera was found");
        }
    }

    private bool TryRender3D()
    {
        //var cameraComponents = _camera3DQuery.FirstOrDefault();
        //if (cameraComponents is null)
        //{
        //    return false;
        //}

        //// todo: this ignores any parent transform
        //var cameraTransform = GlobalTransform.FromTransform(cameraComponents.Component2.LocalTransform);

        //var renderResult = _renderer.Render(cameraTransform, _spriteQuery.Select(x => x.Component2.GlobalTransform));
        //if (renderResult.TryGetError(out var error))
        //{
        //    Console.WriteLine("Failed to render 3d: {0}", error.Error.Error);
        //}

        return false;
    }

    private bool TryRender2D()
    {
        var components = _camera2DQuery.FirstOrDefault();
        if (components is null)
        {
            return false;
        }

        var (camera, transformComponent) = components;

        // todo: this ignores any parent components
        var cameraPosition = transformComponent.LocalTransform.position;

        var lines = new List<IRenderer.LineRender>();
        var sprites = new List<IRenderer.SpriteRender>();
        var screenSprites = new List<IRenderer.SpriteRender>();
        var textRenders = new List<IRenderer.TextRender>();

        var renderCommands = _renderCommandQueue.Flush();
        foreach (var command in renderCommands)
        {
            switch (command)
            {
                case RenderLineCommand renderLineCommand:
                    {
                        lines.Add(new IRenderer.LineRender(renderLineCommand.Start, renderLineCommand.End));
                        break;
                    }
                case RenderSpriteCommand renderSpriteCommand:
                    {
                        sprites.Add(new IRenderer.SpriteRender(
                            renderSpriteCommand.Sprite,
                            renderSpriteCommand.Dimensions,
                            renderSpriteCommand.Transparency,
                            renderSpriteCommand.GlobalTransform.ModelMatrix));
                        break;
                    }
                case RenderScreenSpaceTextCommand renderScreenSpaceTextCommand:
                    {
                        textRenders.Add(new IRenderer.TextRender(
                            renderScreenSpaceTextCommand.Position,
                            renderScreenSpaceTextCommand.Text,
                            renderScreenSpaceTextCommand.Transparency,
                            renderScreenSpaceTextCommand.Texture,
                            LineHeight: 30,
                            SpaceWidth: 20,
                            renderScreenSpaceTextCommand.CharacterSprites));
                        break;
                    }
                case RenderScreenSpaceSpriteCommand renderScreenSpaceSpriteCommand:
                    {
                        screenSprites.Add(new IRenderer.SpriteRender(
                            renderScreenSpaceSpriteCommand.Sprite,
                            renderScreenSpaceSpriteCommand.Sprite.Dimensions,
                            renderScreenSpaceSpriteCommand.Transparency,
                            Matrix4x4.CreateScale(renderScreenSpaceSpriteCommand.Dimensions.Extend(1f))
                            * Matrix4x4.CreateTranslation(renderScreenSpaceSpriteCommand.Position)
                            ));
                        break;
                    }
            }
        }

        var stats = _renderer.RenderOrthographic(
            cameraPosition,
            camera.Size,
            sprites,
            screenSprites,
            lines,
            textRenders);

        _renderStats.DrawCalls = stats.DrawCalls;

        return true;
    }
}

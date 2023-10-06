using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Rendering;
using MyEngine.Utils;

namespace MyEngine.Rendering;

public class RenderSystem : ISystem
{
    private readonly Renderer _renderer;
    private readonly RenderCommandQueue _renderCommandQueue;
    private readonly IQuery<Camera3DComponent, TransformComponent> _camera3DQuery;
    private readonly IQuery<Camera2DComponent, TransformComponent> _camera2DQuery;

    public RenderSystem(
        Renderer renderer,
        IQuery<Camera3DComponent, TransformComponent> camera3DQuery,
        IQuery<Camera2DComponent, TransformComponent> camera2DQuery,
        RenderCommandQueue renderCommandQueue)
    {
        _renderer = renderer;
        _camera3DQuery = camera3DQuery;
        _camera2DQuery = camera2DQuery;
        _renderCommandQueue = renderCommandQueue;
    }

    public void Run(double deltaTime)
    {

        if (!TryRender3D())
        {
            TryRender2D();
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

        var lines = new List<Renderer.LineRender>();
        var sprites = new List<Renderer.SpriteRender>();
        var screenSprites = new List<Renderer.SpriteRender>();
        var textRenders = new List<Renderer.TextRender>();

        var renderCommands = _renderCommandQueue.Flush();
        foreach (var command in renderCommands)
        {
            switch (command)
            {
                case RenderLineCommand renderLineCommand:
                    {
                        lines.Add(new Renderer.LineRender(renderLineCommand.Start, renderLineCommand.End));
                        break;
                    }
                case RenderSpriteCommand renderSpriteCommand:
                    {
                        sprites.Add(new Renderer.SpriteRender(
                            renderSpriteCommand.Sprite,
                            1f,
                            renderSpriteCommand.Sprite.WorldDimensions,
                            renderSpriteCommand.GlobalTransform.ModelMatrix));
                        break;
                    }
                case RenderScreenSpaceTextCommand renderScreenSpaceTextCommand:
                    {
                        textRenders.Add(new Renderer.TextRender(
                            renderScreenSpaceTextCommand.Position,
                            renderScreenSpaceTextCommand.Text,
                            renderScreenSpaceTextCommand.Texture,
                            renderScreenSpaceTextCommand.CharacterSprites));
                        break;
                    }
                case RenderScreenSpaceSpriteCommand renderScreenSpaceSpriteCommand:
                    {
                        screenSprites.Add(new Renderer.SpriteRender(
                            renderScreenSpaceSpriteCommand.Sprite,
                            renderScreenSpaceSpriteCommand.Transparency,
                            renderScreenSpaceSpriteCommand.Dimensions,
                            Matrix4x4.CreateTranslation(renderScreenSpaceSpriteCommand.Position.Extend(0f))));
                        break;
                    }
            }
        }

        _renderer.RenderOrthographic(
            cameraPosition,
            camera.Size,
            sprites,
            screenSprites,
            lines,
            textRenders);

        return true;
    }
}

using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.UI;
using MyEngine.Utils;

namespace MyEngine.Rendering;

public class RenderSystem : ISystem
{
    private readonly Renderer _renderer;
    private readonly ILineRenderResource _lineRenderResource;
    private readonly IQuery<Camera3DComponent, TransformComponent> _camera3DQuery;
    private readonly IQuery<Camera2DComponent, TransformComponent> _camera2DQuery;
    private readonly IQuery<SpriteComponent, TransformComponent> _spriteQuery;
    private readonly IQuery<UICanvasComponent, ChildrenComponent> _canvasQuery;
    private readonly IQuery<UITextComponent, UITransformComponent> _textQuery;

    public RenderSystem(
        Renderer renderer,
        IQuery<Camera3DComponent, TransformComponent> camera3DQuery,
        IQuery<Camera2DComponent, TransformComponent> camera2DQuery,
        IQuery<SpriteComponent, TransformComponent> spriteQuery,
        ILineRenderResource lineRenderResource,
        IQuery<UICanvasComponent, ChildrenComponent> canvasQuery,
        IQuery<UITextComponent, UITransformComponent> textQuery)
    {
        _renderer = renderer;
        _camera3DQuery = camera3DQuery;
        _camera2DQuery = camera2DQuery;
        _spriteQuery = spriteQuery;
        _lineRenderResource = lineRenderResource;
        _canvasQuery = canvasQuery;
        _textQuery = textQuery;
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
        var cameraComponents = _camera3DQuery.FirstOrDefault();
        if (cameraComponents is null)
        {
            return false;
        }

        // todo: this ignores any parent transform
        var cameraTransform = GlobalTransform.FromTransform(cameraComponents.Component2.LocalTransform);

        var renderResult = _renderer.Render(cameraTransform, _spriteQuery.Select(x => x.Component2.GlobalTransform));
        if (renderResult.TryGetError(out var error))
        {
            Console.WriteLine("Failed to render 3d: {0}", error.Error.Error);
        }

        return true;
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

        var lines = _lineRenderResource.FlushLines();

        var textRenders = _canvasQuery.SelectMany(x =>
        {
            return x.Component2.Children.Select(y =>
            {
                var childText = _textQuery.TryGetForEntity(y);
                return childText is null
                    ? ((UITextComponent TextComponent, UITransformComponent TransformComponent)?)null
                    : (childText.Component1, childText.Component2);
            }).WhereNotNull()
            .Select(y => new Renderer.TextRender(y.TransformComponent.Position, y.TextComponent.Text, y.TextComponent.Font));
        });

        _renderer.RenderOrthographic(
            cameraPosition,
            camera.Size,
            _spriteQuery.Select(x => new Renderer.SpriteRender(
                x.Component1.Sprite,
                x.Component2.GlobalTransform)),
            lines.Select(x => new Renderer.LineRender(x.Start, x.End)),
            textRenders);

        return true;
    }
}

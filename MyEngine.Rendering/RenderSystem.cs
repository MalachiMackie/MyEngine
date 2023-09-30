using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.UI;

namespace MyEngine.Rendering;

public class RenderSystem : ISystem
{
    private readonly Renderer _renderer;
    private readonly ILineRenderResource _lineRenderResource;
    private readonly IQuery<Camera3DComponent, TransformComponent> _camera3DQuery;
    private readonly IQuery<Camera2DComponent, TransformComponent> _camera2DQuery;
    private readonly IQuery<SpriteComponent, TransformComponent> _spriteQuery;
    private readonly AssetCollection _assetCollection;
    private readonly IAssetCommands _assetCommands;

    public RenderSystem(
        Renderer renderer,
        IQuery<Camera3DComponent, TransformComponent> camera3DQuery,
        IQuery<Camera2DComponent, TransformComponent> camera2DQuery,
        IQuery<SpriteComponent, TransformComponent> spriteQuery,
        AssetCollection assetCollection,
        ILineRenderResource lineRenderResource,
        IAssetCommands assetCommands)
    {
        _renderer = renderer;
        _camera3DQuery = camera3DQuery;
        _camera2DQuery = camera2DQuery;
        _spriteQuery = spriteQuery;
        _lineRenderResource = lineRenderResource;
        _assetCollection = assetCollection;
        _assetCommands = assetCommands;
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

    private bool _initiatedFontLoading;
    private AssetId? _assetId;
    private FontAsset? _fontAsset;

    private bool TryRender2D()
    {
        var components = _camera2DQuery.FirstOrDefault();
        if (components is null)
        {
            return false;
        }

        if (!_initiatedFontLoading)
        {
            _initiatedFontLoading = true;
            _assetId = _assetCommands.LoadAsset<FontAsset>("Hermit-Regular-fed68123.png");
        }

        if (_fontAsset is null && _assetId is not null)
        {
            var result = _assetCollection.TryGetAsset<FontAsset>(_assetId);
            if (result.IsSuccess)
            {
                _fontAsset = result.Unwrap();
            }
        }

        var (camera, transformComponent) = components;

        // todo: this ignores any parent components
        var cameraPosition = transformComponent.LocalTransform.position;

        var lines = _lineRenderResource.FlushLines();

        _renderer.RenderOrthographic(
            cameraPosition,
            camera.Size,
            _spriteQuery.Select(x => new Renderer.SpriteRender(
                x.Component1.Sprite,
                x.Component2.GlobalTransform)),
            lines.Select(x => new Renderer.Line(x.Start, x.End)).ToArray(),
            _fontAsset);

        return true;
    }
}

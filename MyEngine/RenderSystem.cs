using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Runtime;

internal class RenderSystem : IRenderSystem
{
    private readonly Renderer _renderer;
    private readonly IQuery<Camera3DComponent, TransformComponent> _camera3DQuery;
    private readonly IQuery<Camera2DComponent, TransformComponent> _camera2DQuery;
    private readonly IQuery<SpriteComponent, TransformComponent> _spriteQuery;

    public RenderSystem(
        Renderer renderer,
        IQuery<Camera3DComponent, TransformComponent> camera3DQuery,
        IQuery<Camera2DComponent, TransformComponent> camera2DQuery,
        IQuery<SpriteComponent, TransformComponent> spriteQuery)
    {
        _renderer = renderer;
        _camera3DQuery = camera3DQuery;
        _camera2DQuery = camera2DQuery;
        _spriteQuery = spriteQuery;
    }

    public void Render(double deltaTime)
    {
        if (!TryRender3D())
        {
            TryRender2D();
        }

    }

    private bool TryRender3D()
    {
        var components = _camera3DQuery.FirstOrDefault();
        if (components is null)
        {
            return false;
        }

        _renderer.Render(components.Component2.Transform, _spriteQuery.Select(x => x.Component2.Transform));

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

        _renderer.RenderOrthographic(transformComponent.Transform.position, camera.Size, _spriteQuery.Select(x => x.Component2.Transform));

        return true;
    }
}

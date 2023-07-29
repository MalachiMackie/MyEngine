using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Runtime
{
    internal class RenderSystem : IRenderSystem
    {
        private readonly Renderer _renderer;
        private readonly MyQuery<Camera3DComponent, TransformComponent> _camera3DQuery;
        private readonly MyQuery<Camera2DComponent, TransformComponent> _camera2DQuery;
        private readonly MyQuery<SpriteComponent, TransformComponent> _spriteQuery;

        public RenderSystem(
            Renderer renderer,
            MyQuery<Camera3DComponent, TransformComponent> camera3DQuery,
            MyQuery<Camera2DComponent, TransformComponent> camera2DQuery,
            MyQuery<SpriteComponent, TransformComponent> spriteQuery)
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
            var (_, transformComponent) = _camera3DQuery.FirstOrDefault();
            if (transformComponent is null)
            {
                return false;
            }

            _renderer.Render(transformComponent.Transform, _spriteQuery.Select(x => x.Item2.Transform));

            return true;
        }

        private bool TryRender2D()
        {
            var (camera, transformComponent) = _camera2DQuery.FirstOrDefault();
            if (camera is null)
            {
                return false;
            }

            _renderer.RenderOrthographic(transformComponent.Transform.position, camera.Size, _spriteQuery.Select(x => x.Item2.Transform));

            return true;
        }
    }
}

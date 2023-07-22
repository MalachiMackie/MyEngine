using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Runtime
{
    internal class RenderSystem : IRenderSystem
    {
        private readonly Renderer _renderer;
        private readonly MyQuery<CameraComponent, TransformComponent> _cameraQuery;
        private readonly MyQuery<SpriteComponent, TransformComponent> _spriteQuery;

        public RenderSystem(
            Renderer renderer,
            MyQuery<CameraComponent, TransformComponent> cameraQuery,
            MyQuery<SpriteComponent, TransformComponent> spriteQuery)
        {
            _renderer = renderer;
            _cameraQuery = cameraQuery;
            _spriteQuery = spriteQuery;
        }

        public void Render(double deltaTime)
        {
            var (_, transformComponent) = _cameraQuery.FirstOrDefault();
            if (transformComponent is null)
            {
                return;
            }

            _renderer.Render(transformComponent.Transform, _spriteQuery.Select(x => x.Item2.Transform));
        }
    }
}

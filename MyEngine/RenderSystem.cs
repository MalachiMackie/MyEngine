using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Runtime
{
    internal class RenderSystem : IRenderSystem<CameraComponent, TransformComponent>
    {
        private Renderer _renderer;

        public RenderSystem(Renderer renderer)
        {
            _renderer = renderer;
        }

        public void Render(double deltaTime, CameraComponent _, TransformComponent transform)
        {
            _renderer.Render(transform.Transform);
        }
    }
}

using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Runtime
{
    internal class AddCameraStartupSystem : IStartupSystem
    {
        private readonly ComponentContainerResource _componentContainer;
        private readonly EntityContainerResource _entityContainer;

        public AddCameraStartupSystem(
            ComponentContainerResource componentContainer,
            EntityContainerResource entityContainer)
        {
            _componentContainer = componentContainer;
            _entityContainer = entityContainer;
        }

        public void Run()
        {
            var entity = new Entity();
            _entityContainer.AddEntity(entity);
            _componentContainer.AddComponent(new CameraComponent(entity.Id));
            _componentContainer.AddComponent(new TransformComponent(entity.Id));
        }
    }
}

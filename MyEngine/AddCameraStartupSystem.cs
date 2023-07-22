using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Runtime
{
    internal class AddCameraStartupSystem : IStartupSystem
    {
        private readonly ComponentContainerResource<CameraComponent> _cameraComponentContainer;
        private readonly ComponentContainerResource<TransformComponent> _transformComponentContainer;
        private readonly EntityContainerResource _entityContainer;

        public AddCameraStartupSystem(
            ComponentContainerResource<CameraComponent> cameraComponentContainer,
            ComponentContainerResource<TransformComponent> transformComponents,
            EntityContainerResource entityContainer)
        {
            _cameraComponentContainer = cameraComponentContainer;
            _transformComponentContainer = transformComponents;
            _entityContainer = entityContainer;
        }

        public void Run()
        {
            var entity = new Entity();
            _entityContainer.AddEntity(entity);
            _cameraComponentContainer.AddComponent(new CameraComponent(entity.Id));
            _transformComponentContainer.AddComponent(new TransformComponent(entity.Id));
        }
    }
}

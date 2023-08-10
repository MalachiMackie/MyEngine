using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using System.Numerics;

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
            var entity = EntityId.Generate();
            _entityContainer.AddEntity(entity);
            _componentContainer.AddComponent(entity, new Camera2DComponent(new Vector2(8f, 4.5f)));
            _componentContainer.AddComponent(entity, new TransformComponent());
        }
    }
}

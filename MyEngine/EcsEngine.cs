using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyGame.Systems;
using System.Diagnostics;

namespace MyEngine.Runtime
{
    internal partial class EcsEngine
    {
        public partial void Update(double dt);

        public partial void Render(double dt);

        public partial void RegisterResource<T>(T resource) where T : IResource;

        public partial void Startup();
    }

    // todo: source generate this
    internal partial class EcsEngine
    {
        // resources
        private readonly ResourceContainer _resourceContainer = new();

        // entities
        private readonly HashSet<EntityId> _entities = new();

        // components
        private readonly ComponentCollection _components = new();

        // systems
        private CameraMovementSystem? _cameraMovementSystem;
        private InputSystem? _inputSystem;
        private QuitOnEscapeSystem? _quitOnEscapeSystem;

        // render systems
        private RenderSystem? _renderSystem;

        public partial void Startup()
        {
            RegisterResource(new EntityContainerResource());
            RegisterResource(new ComponentContainerResource());

            {
                if (_resourceContainer.TryGetResource<EntityContainerResource>(out var entityContainer)
                    && _resourceContainer.TryGetResource<ComponentContainerResource>(out var componentContainer))
                {
                    new AddCameraStartupSystem(componentContainer, entityContainer)
                        .Run();
                }
            }
        }

        public partial void Render(double dt)
        {
            _renderSystem?.Render(dt);
        }


        public partial void Update(double dt)
        {
            _inputSystem?.Run(dt);
            _cameraMovementSystem?.Run(dt);
            _quitOnEscapeSystem?.Run(dt);

            AddNewEntities();
            AddNewComponents();
        }

        private void AddNewEntities()
        {
            Debug.Assert(_resourceContainer.TryGetResource<EntityContainerResource>(out var entityContainer));
            while (entityContainer.NewEntities.TryDequeue(out var entity))
            {
                if (!_entities.Add(entity.Id))
                {
                    throw new InvalidOperationException("Cannot add the same entity multiple times");
                }
            }
        }

        private void AddNewComponents()
        {
            Debug.Assert(_resourceContainer.TryGetResource<ComponentContainerResource>(out var components));
            while (components.NewComponents.TryDequeue(out var component))
            {
                _components.AddComponent(component);
            }
        }

        public partial void RegisterResource<T>(T resource) where T : IResource
        {
            _resourceContainer.RegisterResource(resource);
            {
                if (_cameraMovementSystem is null
                    && _resourceContainer.TryGetResource<InputResource>(out var inputResource))
                {
                    IEnumerable<(CameraComponent, TransformComponent)> GetComponents()
                    {
                        foreach (var entityId in _entities)
                        {
                            if (_components.TryGetComponent<TransformComponent>(entityId, out var transformComponent)
                                && _components.TryGetComponent<CameraComponent>(entityId, out var cameraComponent))
                            {
                                yield return (cameraComponent, transformComponent);
                            }
                        }
                    }
                    _cameraMovementSystem = new CameraMovementSystem(inputResource, new MyQuery<CameraComponent, TransformComponent>(GetComponents));
                }
            }
            {
                if (_inputSystem is null
                    && _resourceContainer.TryGetResource<InputResource>(out var inputResource)
                    && _resourceContainer.TryGetResource<MyInput>(out var myInput))
                {
                    _inputSystem = new InputSystem(myInput, inputResource);
                }
            }
            {
                if (_renderSystem is null
                    && _resourceContainer.TryGetResource<Renderer>(out var renderer))
                {
                    IEnumerable<(CameraComponent, TransformComponent)> GetComponents()
                    {
                        foreach (var entityId in _entities)
                        {
                            if (_components.TryGetComponent<TransformComponent>(entityId, out var transformComponent)
                                && _components.TryGetComponent<CameraComponent>(entityId, out var cameraComponent))
                            {
                                yield return (cameraComponent, transformComponent);
                            }
                        }
                    }
                    _renderSystem = new RenderSystem(renderer, new MyQuery<CameraComponent, TransformComponent>(GetComponents));
                }
            }
            {
                if (_quitOnEscapeSystem is null
                    && _resourceContainer.TryGetResource<MyWindow>(out var window)
                    && _resourceContainer.TryGetResource<InputResource>(out var inputResource))
                {
                    _quitOnEscapeSystem = new QuitOnEscapeSystem(window, inputResource);
                }
            }
        }
    }
}

﻿using MyEngine.Core.Ecs;
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
        private HashSet<EntityId> _entities = new();

        // components
        private ComponentCollection<TransformComponent> _transformComponents = new();
        private ComponentCollection<CameraComponent> _cameraComponents = new();

        // systems
        private CameraMovementSystem? _cameraMovementSystem;
        private InputSystem? _inputSystem;
        private QuitOnEscapeSystem? _quitOnEscapeSystem;

        // render systems
        private RenderSystem? _renderSystem;

        public partial void Startup()
        {
            RegisterResource(new EntityContainerResource());
            RegisterResource(new ComponentContainerResource<TransformComponent>());
            RegisterResource(new ComponentContainerResource<CameraComponent>());

            {
                if (_resourceContainer.TryGetResource<EntityContainerResource>(out var entityContainerResource)
                    && _resourceContainer.TryGetResource<ComponentContainerResource<TransformComponent>>(out var transformComponents)
                    && _resourceContainer.TryGetResource<ComponentContainerResource<CameraComponent>>(out var cameraComponents))
                {
                    new AddCameraStartupSystem(cameraComponents, transformComponents, entityContainerResource)
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
            AddComponents(_transformComponents);
            AddComponents(_cameraComponents);
        }

        private void AddComponents<T>(ComponentCollection<T> componentCollection)
            where T : IComponent
        {
            Debug.Assert(_resourceContainer.TryGetResource<ComponentContainerResource<T>>(out var components));
            while (components.NewComponents.TryDequeue(out var component))
            {
                if (!_entities.Contains(component.EntityId))
                {
                    throw new InvalidOperationException("Cannot add a component before its entity");
                }
                componentCollection.AddComponent(component);
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
                            if (_transformComponents.TryGetComponents(entityId, out var transforms))
                            {
                                if (_cameraComponents.TryGetComponents(entityId, out var cameraComponents))
                                {
                                    yield return (cameraComponents[0], transforms[0]);
                                }
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
                            if (_transformComponents.TryGetComponents(entityId, out var transforms))
                            {
                                if (_cameraComponents.TryGetComponents(entityId, out var cameraComponents))
                                {
                                    yield return (cameraComponents[0], transforms[0]);
                                }
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

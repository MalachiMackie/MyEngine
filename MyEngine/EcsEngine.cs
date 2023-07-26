using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Physics;
using MyGame;
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
        private AddSpritesSystem? _addSpritesSystem;
        private PhysicsSystem? _physicsSystem;
        private ApplyImpulseSystem? _applyImpulseSystem;

        // render systems
        private RenderSystem? _renderSystem;

        public partial void Startup()
        {
            RegisterResource(new EntityContainerResource());
            RegisterResource(new ComponentContainerResource());
            RegisterResource(new PhysicsResource());
            RegisterResource(new MyPhysics());

            {
                if (_resourceContainer.TryGetResource<EntityContainerResource>(out var entityContainer)
                    && _resourceContainer.TryGetResource<ComponentContainerResource>(out var componentContainer))
                {
                    new AddCameraStartupSystem(componentContainer, entityContainer)
                        .Run();
                    new AddStartupSpritesSystem(entityContainer, componentContainer)
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

            _physicsSystem?.Run(dt);

            _cameraMovementSystem?.Run(dt);
            _quitOnEscapeSystem?.Run(dt);
            _addSpritesSystem?.Run(dt);
            _applyImpulseSystem?.Run(dt);


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
                    _cameraMovementSystem = new CameraMovementSystem(inputResource, CreateQuery<CameraComponent, TransformComponent>());
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
                    _renderSystem = new RenderSystem(renderer,
                        CreateQuery<CameraComponent, TransformComponent>(),
                        CreateQuery<SpriteComponent, TransformComponent>());
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
            {
                if (_addSpritesSystem is null
                    && _resourceContainer.TryGetResource<InputResource>(out var inputResource)
                    && _resourceContainer.TryGetResource<EntityContainerResource>(out var entityContainer)
                    && _resourceContainer.TryGetResource<ComponentContainerResource>(out var componentContainer))
                {
                    _addSpritesSystem = new AddSpritesSystem(
                        inputResource,
                        entityContainer,
                        componentContainer,
                        CreateQuery<SpriteComponent, TransformComponent>());
                }
            }
            {
                if (_physicsSystem is null
                    && _resourceContainer.TryGetResource<PhysicsResource>(out var physicsResource)
                    && _resourceContainer.TryGetResource<MyPhysics>(out var myPhysics))
                {
                    _physicsSystem = new PhysicsSystem(
                        physicsResource,
                        myPhysics,
                        CreateQuery<TransformComponent, StaticBody2DComponent, BoxCollider2DComponent>(),
                        CreateQuery<TransformComponent, DynamicBody2DComponent, BoxCollider2DComponent>());
                }
            }
            {
                if (_applyImpulseSystem is null
                    && _resourceContainer.TryGetResource<InputResource>(out var inputResource)
                    && _resourceContainer.TryGetResource<PhysicsResource>(out var physicsResource))
                {
                    _applyImpulseSystem = new ApplyImpulseSystem(
                        physicsResource,
                        inputResource,
                        CreateQuery<PlayerComponent>());
                }
            }
        }


        private IEnumerable<T> GetComponents<T>()
            where T : class, IComponent
        {
            foreach (var entityId in _entities)
            {
                if (_components.TryGetComponent<T>(entityId, out var component))
                {
                    yield return component;
                }
            }
        }

        private MyQuery<T> CreateQuery<T>()
            where T : class, IComponent
        {
            return new MyQuery<T>(GetComponents<T>);
        }

        private MyQuery<T1, T2> CreateQuery<T1, T2>()
            where T1 : class, IComponent
            where T2 : class, IComponent
        {
            return new MyQuery<T1, T2>(GetComponents<T1, T2>);
        }

        private MyQuery<T1, T2, T3> CreateQuery<T1, T2, T3>()
            where T1 : class, IComponent
            where T2 : class, IComponent
            where T3 : class, IComponent
        {
            return new MyQuery<T1, T2, T3>(GetComponents<T1, T2, T3>);
        }

        private IEnumerable<(T1, T2)> GetComponents<T1, T2>()
            where T1 : class, IComponent
            where T2 : class, IComponent
        {
            foreach (var entityId in _entities)
            {
                if (_components.TryGetComponent<T1>(entityId, out var component1)
                    && _components.TryGetComponent<T2>(entityId, out var component2))
                {
                    yield return (component1, component2);
                }
            }
        }

        private IEnumerable<(T1, T2, T3)> GetComponents<T1, T2, T3>()
            where T1 : class, IComponent
            where T2 : class, IComponent
            where T3 : class, IComponent
        {
            foreach (var entityId in _entities)
            {
                if (_components.TryGetComponent<T1>(entityId, out var component1)
                    && _components.TryGetComponent<T2>(entityId, out var component2)
                    && _components.TryGetComponent<T3>(entityId, out var component3))
                {
                    yield return (component1, component2, component3);
                }
            }
        }
    }
}

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
        private RotatePlayerSystem? _rotatePlayerSystem;
        private ToggleSpriteSystem? _toggleSpriteSystem;
        private OnCollisionSystem? _onCollisionSystem;

        // render systems
        private RenderSystem? _renderSystem;

        public EcsEngine()
        {
            AddSystemInstantiations();
        }

        public partial void Startup()
        {
            RegisterResource(new EntityContainerResource());
            RegisterResource(new ComponentContainerResource());
            RegisterResource(new PhysicsResource());
            RegisterResource(new MyPhysics());
            RegisterResource(new CollisionsResource());

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
            _rotatePlayerSystem?.Run(dt);
            _toggleSpriteSystem?.Run(dt);
            _onCollisionSystem?.Run(dt);

            // todo: do users expect components/entities to be removed from the scene immediately?
            RemoveComponents();
            RemoveEntities();

            AddNewEntities();
            AddNewComponents();
        }

        private void AddNewEntities()
        {
            Debug.Assert(_resourceContainer.TryGetResource<EntityContainerResource>(out var entityContainer));
            while (entityContainer.NewEntities.TryDequeue(out var entity))
            {
                if (!_entities.Add(entity))
                {
                    throw new InvalidOperationException("Cannot add the same entity multiple times");
                }
            }
        }

        private void RemoveEntities()
        {
            Debug.Assert(_resourceContainer.TryGetResource<EntityContainerResource>(out var entityContainer));
            while (entityContainer.DeleteEntities.TryDequeue(out var entity))
            {
                _components.DeleteComponentsForEntity(entity);
                _entities.Remove(entity);
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

        private void RemoveComponents()
        {
            Debug.Assert(_resourceContainer.TryGetResource<ComponentContainerResource>(out var components));
            while (components.RemoveComponents.TryDequeue(out var removeComponent))
            {
                _components.DeleteComponent(removeComponent.EntityId, removeComponent.ComponentType);
            }
        }

        private readonly Dictionary<Type, Action> _systemInstantiations = new();

        private void AddSystemInstantiations()
        {
            _systemInstantiations.Add(typeof(CameraMovementSystem), () =>
            {
                if (_resourceContainer.TryGetResource<InputResource>(out var inputResource))
                {
                    _cameraMovementSystem = new CameraMovementSystem(
                        inputResource,
                        CreateQuery<Camera3DComponent, TransformComponent>(),
                        CreateQuery<Camera2DComponent, TransformComponent>());
                    _uninstantiatedSystems.Remove(typeof(CameraMovementSystem));
                }
            });

            _systemInstantiations.Add(typeof(InputSystem), () =>
            {
                if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                    && _resourceContainer.TryGetResource<MyInput>(out var myInput))
                {
                    _inputSystem = new InputSystem(myInput, inputResource);
                    _uninstantiatedSystems.Remove(typeof(InputSystem));
                }
            });

            _systemInstantiations.Add(typeof(RenderSystem), () =>
            {
                if (_resourceContainer.TryGetResource<Renderer>(out var renderer))
                {
                    _renderSystem = new RenderSystem(
                        renderer,
                        CreateQuery<Camera3DComponent, TransformComponent>(),
                        CreateQuery<Camera2DComponent, TransformComponent>(),
                        CreateQuery<SpriteComponent, TransformComponent>());
                    _uninstantiatedSystems.Remove(typeof(RenderSystem));
                }
            });

            _systemInstantiations.Add(typeof(QuitOnEscapeSystem), () =>
            {
                if (_resourceContainer.TryGetResource<MyWindow>(out var window)
                    && _resourceContainer.TryGetResource<InputResource>(out var inputResource))
                {
                    _quitOnEscapeSystem = new QuitOnEscapeSystem(window, inputResource);
                    _uninstantiatedSystems.Remove(typeof(QuitOnEscapeSystem));
                }
            });

            _systemInstantiations.Add(typeof(AddSpritesSystem), () =>
            {
                if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                    && _resourceContainer.TryGetResource<EntityContainerResource>(out var entityContainer)
                    && _resourceContainer.TryGetResource<ComponentContainerResource>(out var componentContainer))
                {
                    _addSpritesSystem = new AddSpritesSystem(
                        inputResource,
                        entityContainer,
                        componentContainer,
                        CreateQuery<SpriteComponent, TransformComponent>());
                    _uninstantiatedSystems.Remove(typeof(AddSpritesSystem));
                }
            });

            _systemInstantiations.Add(typeof(PhysicsSystem), () =>
            {
                IEnumerable<(TransformComponent, StaticBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>)> GetQuery1Components()
                {
                    foreach (var entityId in _entities)
                    {
                        if (_components.TryGetComponent<TransformComponent>(entityId, out var transformComponent)
                            && _components.TryGetComponent<StaticBody2DComponent>(entityId, out var staticBodyComponent)
                            && _components.TryGetComponent<Collider2DComponent>(entityId, out var collider2dComponent))
                        {
                            var physicsMaterial = _components.GetOptionalComponent<PhysicsMaterial>(entityId);
                            yield return (transformComponent, staticBodyComponent, collider2dComponent, physicsMaterial);
                        }
                    }
                }

                IEnumerable<(TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>)> GetQuery2Components()
                {
                    foreach (var entityId in _entities)
                    {
                        if (_components.TryGetComponent<TransformComponent>(entityId, out var transformComponent)
                            && _components.TryGetComponent<DynamicBody2DComponent>(entityId, out var dynamicBodyComponent)
                            && _components.TryGetComponent<Collider2DComponent>(entityId, out var collider2DComponent))
                        {
                            var physicsMaterial = _components.GetOptionalComponent<PhysicsMaterial>(entityId);
                            yield return (transformComponent, dynamicBodyComponent, collider2DComponent, physicsMaterial);
                        }
                    }
                }

                if (_resourceContainer.TryGetResource<PhysicsResource>(out var physicsResource)
                    && _resourceContainer.TryGetResource<MyPhysics>(out var myPhysics)
                    && _resourceContainer.TryGetResource<CollisionsResource>(out var collisionsResource))
                {
                    _physicsSystem = new PhysicsSystem(
                        physicsResource,
                        collisionsResource,
                        myPhysics,
                        new MyQuery<TransformComponent, StaticBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>>(GetQuery1Components),
                        new MyQuery<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>>(GetQuery2Components));
                    _uninstantiatedSystems.Remove(typeof(PhysicsSystem));
                }
            });

            _systemInstantiations.Add(typeof(ApplyImpulseSystem), () =>
            {
                if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                    && _resourceContainer.TryGetResource<PhysicsResource>(out var physicsResource))
                {
                    _applyImpulseSystem = new ApplyImpulseSystem(
                        physicsResource,
                        inputResource,
                        CreateQuery<PlayerComponent>());
                    _uninstantiatedSystems.Remove(typeof(ApplyImpulseSystem));
                }
            });

            _systemInstantiations.Add(typeof(RotatePlayerSystem), () =>
            {
                if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                    && _resourceContainer.TryGetResource<PhysicsResource>(out var physicsResource))
                {
                    _rotatePlayerSystem = new RotatePlayerSystem(
                        CreateQuery<PlayerComponent>(),
                        physicsResource,
                        inputResource);
                    _uninstantiatedSystems.Remove(typeof(RotatePlayerSystem));
                }
            });

            _systemInstantiations.Add(typeof(ToggleSpriteSystem), () =>
            {
                if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                    && _resourceContainer.TryGetResource<ComponentContainerResource>(out var componentContainer))
                {
                    _toggleSpriteSystem = new ToggleSpriteSystem(
                        CreateQuery<PlayerComponent>(),
                        CreateQuery<PlayerComponent, SpriteComponent>(),
                        componentContainer,
                        inputResource);
                    _uninstantiatedSystems.Remove(typeof(ToggleSpriteSystem));
                }
            });

            _systemInstantiations.Add(typeof(OnCollisionSystem), () =>
            {
                if (_resourceContainer.TryGetResource<CollisionsResource>(out var collisionsResource)
                    && _resourceContainer.TryGetResource<EntityContainerResource>(out var entityContainerResource))
                {
                    _onCollisionSystem = new OnCollisionSystem(
                        collisionsResource,
                        CreateQuery<TestComponent>(),
                        entityContainerResource);
                }
            });
        }

        /// <summary>
        /// Dictionary of uninstantiated systems, and the list of resource dependencies they have
        /// </summary>
        private Dictionary<Type, Type[]> _uninstantiatedSystems = new Dictionary<Type, Type[]>
        {
            { typeof(CameraMovementSystem), new [] { typeof(InputResource) } },
            { typeof(InputSystem), new[] { typeof(InputResource), typeof(MyInput) } },
            { typeof(RenderSystem), new[] { typeof(Renderer) } },
            { typeof(QuitOnEscapeSystem), new[] { typeof(InputResource), typeof(MyWindow) } },
            { typeof(AddSpritesSystem), new[] { typeof(InputResource), typeof(EntityContainerResource), typeof(ComponentContainerResource) } },
            { typeof(PhysicsSystem), new[] { typeof(PhysicsResource), typeof(CollisionsResource), typeof(MyPhysics) } },
            { typeof(ApplyImpulseSystem), new[] { typeof(InputResource), typeof(PhysicsResource) } },
            { typeof(RotatePlayerSystem), new[] { typeof(InputResource), typeof(PhysicsResource) } },
            { typeof(ToggleSpriteSystem), new[] { typeof(InputResource), typeof(ComponentContainerResource) } },
            { typeof(OnCollisionSystem), new [] { typeof(CollisionsResource) } }
        };

        public partial void RegisterResource<T>(T resource) where T : IResource
        {
            _resourceContainer.RegisterResource(resource);
            foreach (var (systemType, resourceTypes) in _uninstantiatedSystems)
            {
                if (resourceTypes.Contains(typeof(T)))
                {
                    _systemInstantiations[systemType].Invoke();
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

        private MyQuery<T1, T2, T3, T4> CreateQuery<T1, T2, T3, T4>()
            where T1 : class, IComponent
            where T2 : class, IComponent
            where T3 : class, IComponent
            where T4 : class, IComponent
        {
            return new MyQuery<T1, T2, T3, T4>(GetComponents<T1, T2, T3, T4>);
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

        private IEnumerable<(T1, T2, T3, T4)> GetComponents<T1, T2, T3, T4>()
            where T1 : class, IComponent
            where T2 : class, IComponent
            where T3 : class, IComponent
            where T4 : class, IComponent
        {
            foreach (var entityId in _entities)
            {
                if (_components.TryGetComponent<T1>(entityId, out var component1)
                    && _components.TryGetComponent<T2>(entityId, out var component2)
                    && _components.TryGetComponent<T3>(entityId, out var component3)
                    && _components.TryGetComponent<T4>(entityId, out var component4))
                {
                    yield return (component1, component2, component3, component4);
                }
            }
        }
    }
}

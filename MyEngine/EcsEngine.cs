using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Physics;
using MyGame.Components;
using MyGame.Resources;
using MyGame.Systems;
using System.Diagnostics;

namespace MyEngine.Runtime;

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
    private OnCollisionSystem? _onCollisionSystem;
    private KinematicVelocitySystem? _kinematicVelocitySystem;
    private MoveBallSystem? _moveBallSystem;
    private KinematicBounceSystem? _kinematicBounceSystem;
    private BallOutOfBoundsSystem? _ballOutOfBoundsSystem;
    private LogBallPositionSystem? _logBallPositionSystem;

    // render systems
    private RenderSystem? _renderSystem;

    public EcsEngine()
    {
        AddSystemInstantiations();
    }

    public partial void Startup()
    {
        RegisterResource(new ResourceRegistrationResource());
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
                if (_resourceContainer.TryGetResource<ResourceRegistrationResource>(out var resourceRegistrationResource))
                {
                    new AddStartupSpritesSystem(entityContainer, componentContainer, resourceRegistrationResource)
                        .Run();
                }
            }
        }

        foreach (var (systemType, _) in _uninstantiatedSystems
            .Where(x => x.Value.Length == 0)
            .ToArray())
        {
            // todo: currently each system instantiation will remove itself from `_uninstantiatedSystems`. I want to find a clearer way to do that.
            // it feels like a weird side effect rather than a clear pattern
            _systemInstantiations[systemType].Invoke();
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
        _kinematicVelocitySystem?.Run(dt);
        _kinematicBounceSystem?.Run(dt);

        _cameraMovementSystem?.Run(dt);
        _quitOnEscapeSystem?.Run(dt);
        _addSpritesSystem?.Run(dt);
        _applyImpulseSystem?.Run(dt);
        _rotatePlayerSystem?.Run(dt);
        _onCollisionSystem?.Run(dt);
        _moveBallSystem?.Run(dt);
        _ballOutOfBoundsSystem?.Run(dt);
        _logBallPositionSystem?.Run(dt);

        // todo: do users expect components/entities to be removed from the scene immediately?
        RemoveComponents();
        RemoveEntities();

        AddNewEntities();
        AddNewComponents();
        AddResources();
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
            _components.AddComponent(component.EntityId, component.Component);
        }
    }

    private void AddResources()
    {
        Debug.Assert(_resourceContainer.TryGetResource<ResourceRegistrationResource>(out var resourceRegistration));
        while (resourceRegistration.Registrations.TryDequeue(out var resource))
        {
            _resourceContainer.RegisterResource(resource.Key, resource.Value);

            foreach (var (systemType, resourceTypes) in _uninstantiatedSystems)
            {
                if (resourceTypes.Contains(resource.Key))
                {
                    _systemInstantiations[systemType].Invoke();
                }
            }
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
                    GetQuery<Camera3DComponent, TransformComponent>(),
                    GetQuery<Camera2DComponent, TransformComponent>());
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
                    GetQuery<Camera3DComponent, TransformComponent>(),
                    GetQuery<Camera2DComponent, TransformComponent>(),
                    GetQuery<SpriteComponent, TransformComponent>());
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
                    GetQuery<SpriteComponent, TransformComponent>());
                _uninstantiatedSystems.Remove(typeof(AddSpritesSystem));
            }
        });

        _systemInstantiations.Add(typeof(PhysicsSystem), () =>
        {
            EntityComponents<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>>? GetQueryComponents(EntityId entityId)
            {
                if (_components.TryGetComponent<TransformComponent>(entityId, out var transformComponent)
                    && _components.TryGetComponent<DynamicBody2DComponent>(entityId, out var dynamicBodyComponent)
                    && _components.TryGetComponent<Collider2DComponent>(entityId, out var collider2DComponent))
                {
                    var physicsMaterial = _components.GetOptionalComponent<PhysicsMaterial>(entityId);
                    return new EntityComponents<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>>(entityId)
                    {
                        Component1 = transformComponent,
                        Component2 = dynamicBodyComponent,
                        Component3 = collider2DComponent,
                        Component4 = physicsMaterial
                    };
                }

                return null;
            }


            if (_resourceContainer.TryGetResource<PhysicsResource>(out var physicsResource)
                && _resourceContainer.TryGetResource<MyPhysics>(out var myPhysics)
                && _resourceContainer.TryGetResource<CollisionsResource>(out var collisionsResource))
            {
                _physicsSystem = new PhysicsSystem(
                    physicsResource,
                    collisionsResource,
                    myPhysics,
                    GetQuery<TransformComponent, StaticBody2DComponent, Collider2DComponent>(),
                    GetQuery(GetQueryComponents),
                    GetQuery<TransformComponent, KinematicBody2DComponent, Collider2DComponent>());
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
                    GetQuery<BallComponent>());
                _uninstantiatedSystems.Remove(typeof(ApplyImpulseSystem));
            }
        });

        _systemInstantiations.Add(typeof(RotatePlayerSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                && _resourceContainer.TryGetResource<PhysicsResource>(out var physicsResource))
            {
                _rotatePlayerSystem = new RotatePlayerSystem(
                    GetQuery<BallComponent>(),
                    physicsResource,
                    inputResource);
                _uninstantiatedSystems.Remove(typeof(RotatePlayerSystem));
            }
        });

        _systemInstantiations.Add(typeof(OnCollisionSystem), () =>
        {
            if (_resourceContainer.TryGetResource<CollisionsResource>(out var collisionsResource)
                && _resourceContainer.TryGetResource<EntityContainerResource>(out var entityContainerResource))
            {
                _onCollisionSystem = new OnCollisionSystem(
                    collisionsResource);
                _uninstantiatedSystems.Remove(typeof(OnCollisionSystem));
            }
        });

        _systemInstantiations.Add(typeof(KinematicVelocitySystem), () =>
        {
            _kinematicVelocitySystem = new KinematicVelocitySystem(GetQuery<TransformComponent, KinematicBody2DComponent>());
            _uninstantiatedSystems.Remove(typeof(KinematicVelocitySystem));
        });

        _systemInstantiations.Add(typeof(MoveBallSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource))
            {
                _moveBallSystem = new MoveBallSystem(GetQuery<BallComponent, KinematicBody2DComponent>(), inputResource);
                _uninstantiatedSystems.Remove(typeof(MoveBallSystem));
            }
        });

        _systemInstantiations.Add(typeof(KinematicBounceSystem), () =>
        {
            if (_resourceContainer.TryGetResource<CollisionsResource>(out var collisionsResource))
            {
                EntityComponents<KinematicBody2DComponent, OptionalComponent<KinematicReboundComponent>>? GetComponents(EntityId entityId)
                {
                    if (_components.TryGetComponent<KinematicBody2DComponent>(entityId, out var kinematicBody2DComponent))
                    {
                        return new EntityComponents<KinematicBody2DComponent, OptionalComponent<KinematicReboundComponent>>(entityId)
                        {
                            Component1 = kinematicBody2DComponent,
                            Component2 = _components.GetOptionalComponent<KinematicReboundComponent>(entityId)
                        };
                    }

                    return null;
                }

                _kinematicBounceSystem = new KinematicBounceSystem(GetQuery(GetComponents),
                    collisionsResource);
                _uninstantiatedSystems.Remove(typeof(KinematicBounceSystem));
            }
        });

        _systemInstantiations.Add(typeof(BallOutOfBoundsSystem), () =>
        {
            if (_resourceContainer.TryGetResource<WorldSizeResource>(out var worldSizeResource))
            {
                _ballOutOfBoundsSystem = new BallOutOfBoundsSystem(GetQuery<TransformComponent, BallComponent, KinematicBody2DComponent>(),
                    worldSizeResource);
                _uninstantiatedSystems.Remove(typeof(BallOutOfBoundsSystem));
            }
        });

        _systemInstantiations.Add(typeof(LogBallPositionSystem), () =>
        {
            _logBallPositionSystem = new LogBallPositionSystem(GetQuery<LogPositionComponent, TransformComponent>());
            _uninstantiatedSystems.Remove(typeof(LogBallPositionSystem));
        });
    }

    /// <summary>
    /// Dictionary of uninstantiated systems, and the list of resource dependencies they have
    /// </summary>
    private readonly Dictionary<Type, Type[]> _uninstantiatedSystems = new()
    {
        { typeof(CameraMovementSystem), new [] { typeof(InputResource) } },
        { typeof(InputSystem), new[] { typeof(InputResource), typeof(MyInput) } },
        { typeof(RenderSystem), new[] { typeof(Renderer) } },
        { typeof(QuitOnEscapeSystem), new[] { typeof(InputResource), typeof(MyWindow) } },
        { typeof(AddSpritesSystem), new[] { typeof(InputResource), typeof(EntityContainerResource), typeof(ComponentContainerResource) } },
        { typeof(PhysicsSystem), new[] { typeof(PhysicsResource), typeof(CollisionsResource), typeof(MyPhysics) } },
        { typeof(ApplyImpulseSystem), new[] { typeof(InputResource), typeof(PhysicsResource) } },
        { typeof(RotatePlayerSystem), new[] { typeof(InputResource), typeof(PhysicsResource) } },
        { typeof(OnCollisionSystem), new [] { typeof(CollisionsResource) } },
        { typeof(KinematicVelocitySystem), Array.Empty<Type>() },
        { typeof(MoveBallSystem), new [] { typeof(InputResource) } },
        { typeof(KinematicBounceSystem), new [] { typeof(CollisionsResource) } },
        { typeof(BallOutOfBoundsSystem), new [] { typeof(WorldSizeResource) } },
        { typeof(LogBallPositionSystem), Array.Empty<Type>() }
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

    private IQuery<T> GetQuery<T>(Func<EntityId, EntityComponents<T>?>? getEntityFunc = null)
        where T : IComponent
    {
        getEntityFunc ??= TryGetComponentsForEntity<T>;
        return new Query<T>
        {
            GetAllImpl = GetComponentsFunc(getEntityFunc),
            TryGetForEntityImpl = getEntityFunc
        };
    }

    private Func<IEnumerable<EntityComponents<T>>> GetComponentsFunc<T>(Func<EntityId, EntityComponents<T>?> getEntityFunc)
        where T : IComponent
    {
        return () => _entities.Select(getEntityFunc)
            .Where(x => x is not null)
            .Cast<EntityComponents<T>>();
    }

    private EntityComponents<T>? TryGetComponentsForEntity<T>(EntityId entityId)
        where T : IComponent
    {
        if (_components.TryGetComponent<T>(entityId, out var component))
        {
            return new EntityComponents<T>(entityId) { Component = component };
        }

        return null;
    }

    private IQuery<T1, T2> GetQuery<T1, T2>(Func<EntityId, EntityComponents<T1, T2>?>? getEntityFunc = null)
        where T1 : IComponent
        where T2 : IComponent
    {
        getEntityFunc ??= TryGetComponentsForEntity<T1, T2>;
        return new Query<T1, T2>
        {
            GetAllImpl = GetComponentsFunc(getEntityFunc),
            TryGetForEntityImpl = getEntityFunc
        };
    }

    private Func<IEnumerable<EntityComponents<T1, T2>>> GetComponentsFunc<T1, T2>(Func<EntityId, EntityComponents<T1, T2>?> getEntityFunc)
        where T1 : IComponent
        where T2 : IComponent
    {
        return () => _entities.Select(getEntityFunc)
            .Where(x => x is not null)
            .Cast<EntityComponents<T1, T2>>();
    }

    private EntityComponents<T1, T2>? TryGetComponentsForEntity<T1, T2>(EntityId entityId)
        where T1 : IComponent
        where T2 : IComponent
    {
        if (_components.TryGetComponent<T1>(entityId, out var component1)
            && _components.TryGetComponent<T2>(entityId, out var component2))
        {
            return new EntityComponents<T1, T2>(entityId)
            {
                Component1 = component1,
                Component2 = component2
            };
        }

        return null;
    }

    private IQuery<T1, T2, T3> GetQuery<T1, T2, T3>(Func<EntityId, EntityComponents<T1, T2, T3>?>? getEntityFunc = null)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        getEntityFunc ??= TryGetComponentsForEntity<T1, T2, T3>;
        return new Query<T1, T2, T3>
        {
            GetAllImpl = GetComponentsFunc(getEntityFunc),
            TryGetForEntityImpl = getEntityFunc
        };
    }

    private Func<IEnumerable<EntityComponents<T1, T2, T3>>> GetComponentsFunc<T1, T2, T3>(Func<EntityId, EntityComponents<T1, T2, T3>?> getEntityFunc)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        return () => _entities.Select(getEntityFunc)
            .Where(x => x is not null)
            .Cast<EntityComponents<T1, T2, T3>>();
    }

    private EntityComponents<T1, T2, T3>? TryGetComponentsForEntity<T1, T2, T3>(EntityId entityId)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        if (_components.TryGetComponent<T1>(entityId, out var component1)
            && _components.TryGetComponent<T2>(entityId, out var component2)
            && _components.TryGetComponent<T3>(entityId, out var component3))
        {
            return new EntityComponents<T1, T2, T3>(entityId)
            {
                Component1 = component1,
                Component2 = component2,
                Component3 = component3
            };
        }

        return null;
    }

    private IQuery<T1, T2, T3, T4> GetQuery<T1, T2, T3, T4>(Func<EntityId, EntityComponents<T1, T2, T3, T4>?>? getEntityFunc = null)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        getEntityFunc ??= TryGetComponentsForEntity<T1, T2, T3, T4>;
        return new Query<T1, T2, T3, T4>
        {
            GetAllImpl = GetComponentsFunc(getEntityFunc),
            TryGetForEntityImpl = getEntityFunc
        };
    }

    private Func<IEnumerable<EntityComponents<T1, T2, T3, T4>>> GetComponentsFunc<T1, T2, T3, T4>(Func<EntityId, EntityComponents<T1, T2, T3, T4>?> getEntityFunc)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        return () => _entities.Select(getEntityFunc)
            .Where(x => x is not null)
            .Cast<EntityComponents<T1, T2, T3, T4>>();
    }

    private EntityComponents<T1, T2, T3, T4>? TryGetComponentsForEntity<T1, T2, T3, T4>(EntityId entityId)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        if (_components.TryGetComponent<T1>(entityId, out var component1)
            && _components.TryGetComponent<T2>(entityId, out var component2)
            && _components.TryGetComponent<T3>(entityId, out var component3)
            && _components.TryGetComponent<T4>(entityId, out var component4))
        {
            return new EntityComponents<T1, T2, T3, T4>(entityId)
            {
                Component1 = component1,
                Component2 = component2,
                Component3 = component3,
                Component4 = component4,
            };
        }

        return null;
    }
}

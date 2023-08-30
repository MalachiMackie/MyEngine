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
    private PhysicsSystem? _physicsSystem;
    private ApplyImpulseSystem? _applyImpulseSystem;
    private RotatePlayerSystem? _rotatePlayerSystem;
    private OnCollisionSystem? _onCollisionSystem;
    private LaunchBallSystem? _moveBallSystem;
    private KinematicBounceSystem? _kinematicBounceSystem;
    private ResetBallSystem? _resetBallSystem;
    private LogBallPositionSystem? _logBallPositionSystem;
    private TransformSyncSystem? _transformSyncSystem;
    private MovePaddleSystem? _movePaddleSystem;
    private ColliderDebugDisplaySystem? _colliderDebugDisplaySystem;
    private BrickCollisionSystem? _brickCollisionSystem;

    // render systems
    private RenderSystem? _renderSystem;

    public EcsEngine()
    {
        AddSystemInstantiations();
    }

    // todo: WithoutComponent<T>

    public partial void Startup()
    {
        RegisterResource<IHierarchyCommands>(new HierarchyCommands(_components));
        RegisterResource(new ResourceRegistrationResource());
        RegisterResource<ICommands>(new Commands(_components, _entities));
        RegisterResource(new PhysicsResource());
        RegisterResource(new MyPhysics());
        RegisterResource(new CollisionsResource());
        RegisterResource<ILineRenderResource>(new LineRenderResource());

        {
            if (_resourceContainer.TryGetResource<ICommands>(out var entityContainer))
            {
                new AddCameraStartupSystem(entityContainer)
                    .Run();
                if (_resourceContainer.TryGetResource<ResourceRegistrationResource>(out var resourceRegistrationResource)
                    && _resourceContainer.TryGetResource<IHierarchyCommands>(out var hierarchyCommands))
                {
                    new AddStartupSpritesSystem(
                        entityContainer,
                        resourceRegistrationResource,
                        hierarchyCommands).Run();
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
        _kinematicBounceSystem?.Run(dt);

        _cameraMovementSystem?.Run(dt);
        _quitOnEscapeSystem?.Run(dt);
        _applyImpulseSystem?.Run(dt);
        _rotatePlayerSystem?.Run(dt);
        _onCollisionSystem?.Run(dt);
        _moveBallSystem?.Run(dt);
        _resetBallSystem?.Run(dt);
        _logBallPositionSystem?.Run(dt);
        _movePaddleSystem?.Run(dt);
        _colliderDebugDisplaySystem?.Run(dt);
        _brickCollisionSystem?.Run(dt);

        _transformSyncSystem?.Run(dt);

        AddResources();
    }

    private void AddResources()
    {
        Debug.Assert(_resourceContainer.TryGetResource<ResourceRegistrationResource>(out var resourceRegistration));
        while (resourceRegistration.Registrations.TryDequeue(out var resource))
        {
            if (_resourceContainer.RegisterResource(resource.Key, resource.Value).TryGetError(out var registerResourceError))
            {
                Console.WriteLine("Failed to register resource: {0}", registerResourceError);
                continue;
            }

            foreach (var (systemType, resourceTypes) in _uninstantiatedSystems)
            {
                if (resourceTypes.Contains(resource.Key))
                {
                    _systemInstantiations[systemType].Invoke();
                }
            }
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
            if (_resourceContainer.TryGetResource<Renderer>(out var renderer)
                && _resourceContainer.TryGetResource<ILineRenderResource>(out var lineRenderResource))
            {
                _renderSystem = new RenderSystem(
                    renderer,
                    GetQuery<Camera3DComponent, TransformComponent>(),
                    GetQuery<Camera2DComponent, TransformComponent>(),
                    GetQuery<SpriteComponent, TransformComponent>(),
                    lineRenderResource);
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

        _systemInstantiations.Add(typeof(PhysicsSystem), () =>
        {
            EntityComponents<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>, OptionalComponent<ParentComponent>>? GetQuery2Components(EntityId entityId)
            {
                if (_components.TryGetComponent<TransformComponent>(entityId, out var transformComponent)
                    && _components.TryGetComponent<DynamicBody2DComponent>(entityId, out var dynamicBodyComponent)
                    && _components.TryGetComponent<Collider2DComponent>(entityId, out var collider2DComponent))
                {
                    var physicsMaterial = _components.GetOptionalComponent<PhysicsMaterial>(entityId);
                    return new EntityComponents<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>, OptionalComponent<ParentComponent>>(entityId)
                    {
                        Component1 = transformComponent,
                        Component2 = dynamicBodyComponent,
                        Component3 = collider2DComponent,
                        Component4 = physicsMaterial,
                        Component5 = _components.GetOptionalComponent<ParentComponent>(entityId)
                    };
                }

                return null;
            }

            EntityComponents<TransformComponent, KinematicBody2DComponent, Collider2DComponent, OptionalComponent<ParentComponent>>? GetQuery3Components(EntityId entityId)
            {
                if (_components.TryGetComponent<TransformComponent>(entityId, out var transformComponent)
                    && _components.TryGetComponent<KinematicBody2DComponent>(entityId, out var kinematicBody2DComponent)
                    && _components.TryGetComponent<Collider2DComponent>(entityId, out var collider2DComponent))
                {
                    return new EntityComponents<TransformComponent, KinematicBody2DComponent, Collider2DComponent, OptionalComponent<ParentComponent>>(entityId)
                    {
                        Component1 = transformComponent,
                        Component2 = kinematicBody2DComponent,
                        Component3 = collider2DComponent,
                        Component4 = _components.GetOptionalComponent<ParentComponent>(entityId)
                    };
                }

                return null;
            }

            EntityComponents<TransformComponent, OptionalComponent<ParentComponent>>? GetQuery4Components(EntityId entityId)
            {
                if (_components.TryGetComponent<TransformComponent>(entityId, out var component1))
                {
                    return new EntityComponents<TransformComponent, OptionalComponent<ParentComponent>>(entityId)
                    {
                        Component1 = component1,
                        Component2 = _components.GetOptionalComponent<ParentComponent>(entityId)
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
                    GetQuery(GetQuery2Components),
                    GetQuery(GetQuery3Components),
                    GetQuery(GetQuery4Components));
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
                && _resourceContainer.TryGetResource<ICommands>(out var entityContainerResource))
            {
                _onCollisionSystem = new OnCollisionSystem(
                    collisionsResource);
                _uninstantiatedSystems.Remove(typeof(OnCollisionSystem));
            }
        });

        _systemInstantiations.Add(typeof(LaunchBallSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                && _resourceContainer.TryGetResource<IHierarchyCommands>(out var hierarchyCommands))
            {
                _moveBallSystem = new LaunchBallSystem(GetQuery<TransformComponent, BallComponent, KinematicBody2DComponent, ParentComponent>(), inputResource, hierarchyCommands);
                _uninstantiatedSystems.Remove(typeof(LaunchBallSystem));
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

        _systemInstantiations.Add(typeof(ResetBallSystem), () =>
        {
            if (_resourceContainer.TryGetResource<WorldSizeResource>(out var worldSizeResource)
                && _resourceContainer.TryGetResource<IHierarchyCommands>(out var hierarchyCommands))
            {
                _resetBallSystem = new ResetBallSystem(GetQuery<TransformComponent, BallComponent, KinematicBody2DComponent>(),
                    worldSizeResource,
                    GetQuery<PaddleComponent, TransformComponent>(),
                    hierarchyCommands);
                _uninstantiatedSystems.Remove(typeof(ResetBallSystem));
            }
        });

        _systemInstantiations.Add(typeof(LogBallPositionSystem), () =>
        {
            _logBallPositionSystem = new LogBallPositionSystem(GetQuery<LogPositionComponent, TransformComponent>());
            _uninstantiatedSystems.Remove(typeof(LogBallPositionSystem));
        });

        _systemInstantiations.Add(typeof(TransformSyncSystem), () =>
        {
            EntityComponents<TransformComponent, OptionalComponent<ParentComponent>, OptionalComponent<ChildrenComponent>>? GetQuery1Components(EntityId entityId)
            {
                if (_components.TryGetComponent<TransformComponent>(entityId, out var component1))
                {
                    return new EntityComponents<TransformComponent, OptionalComponent<ParentComponent>, OptionalComponent<ChildrenComponent>>(entityId)
                    {
                        Component1 = component1,
                        Component2 = _components.GetOptionalComponent<ParentComponent>(entityId),
                        Component3 = _components.GetOptionalComponent<ChildrenComponent>(entityId)
                    };
                }

                return null;
            };

            _transformSyncSystem = new TransformSyncSystem(GetQuery(GetQuery1Components));
            _uninstantiatedSystems.Remove(typeof(TransformSyncSystem));
        });

        _systemInstantiations.Add(typeof(MovePaddleSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource))
            {
                _movePaddleSystem = new MovePaddleSystem(GetQuery<TransformComponent, PaddleComponent>(), inputResource);
                _uninstantiatedSystems.Remove(typeof(MovePaddleSystem));
            }
        });

        _systemInstantiations.Add(typeof(ColliderDebugDisplaySystem), () =>
        {
            if (_resourceContainer.TryGetResource<MyPhysics>(out var myPhysics)
                && _resourceContainer.TryGetResource<ILineRenderResource>(out var lineRenderResource))
            {
                _colliderDebugDisplaySystem = new ColliderDebugDisplaySystem(myPhysics, lineRenderResource);
                _uninstantiatedSystems.Remove(typeof(ColliderDebugDisplaySystem));
            }
        });

        _systemInstantiations.Add(typeof(BrickCollisionSystem), () =>
        {
            if (_resourceContainer.TryGetResource<ICommands>(out var commands)
                && _resourceContainer.TryGetResource<CollisionsResource>(out var collisionsResource))
            {
                _brickCollisionSystem = new BrickCollisionSystem(
                    collisionsResource,
                    GetQuery<BallComponent>(),
                    GetQuery<BrickComponent>(),
                    commands);
                _uninstantiatedSystems.Remove(typeof(BrickCollisionSystem));
            }
        });
    }

    /// <summary>
    /// Dictionary of uninstantiated systems, and the list of resource dependencies they have
    /// </summary>
    private readonly Dictionary<Type, Type[]> _uninstantiatedSystems = new()
    {
        { typeof(CameraMovementSystem), new [] { typeof(InputResource) } },
        { typeof(InputSystem), new[] { typeof(InputResource), typeof(MyInput) } },
        { typeof(RenderSystem), new[] { typeof(Renderer), typeof(ILineRenderResource) } },
        { typeof(QuitOnEscapeSystem), new[] { typeof(InputResource), typeof(MyWindow) } },
        { typeof(PhysicsSystem), new[] { typeof(PhysicsResource), typeof(CollisionsResource), typeof(MyPhysics) } },
        { typeof(ApplyImpulseSystem), new[] { typeof(InputResource), typeof(PhysicsResource) } },
        { typeof(RotatePlayerSystem), new[] { typeof(InputResource), typeof(PhysicsResource) } },
        { typeof(OnCollisionSystem), new [] { typeof(CollisionsResource), typeof(ICommands) } },
        { typeof(LaunchBallSystem), new [] { typeof(InputResource), typeof(IHierarchyCommands) } },
        { typeof(KinematicBounceSystem), new [] { typeof(CollisionsResource) } },
        { typeof(ResetBallSystem), new [] { typeof(WorldSizeResource) } },
        { typeof(LogBallPositionSystem), Array.Empty<Type>() },
        { typeof(TransformSyncSystem), Array.Empty<Type>() },
        { typeof(MovePaddleSystem), new [] { typeof(InputResource) } },
        { typeof(ColliderDebugDisplaySystem), new [] { typeof(MyPhysics), typeof(ILineRenderResource) } },
        { typeof(BrickCollisionSystem), new [] { typeof(ICommands), typeof(CollisionsResource) } }
    };

    public partial void RegisterResource<T>(T resource) where T : IResource
    {
        if (_resourceContainer.RegisterResource(resource).TryGetError(out var error))
        {
            Console.WriteLine("Failed to register resource: {0}", error);
            return;
        }

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

    private IQuery<T1, T2, T3, T4, T5> GetQuery<T1, T2, T3, T4, T5>(Func<EntityId, EntityComponents<T1, T2, T3, T4, T5>?>? getEntityFunc = null)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        getEntityFunc ??= TryGetComponentsForEntity<T1, T2, T3, T4, T5>;
        return new Query<T1, T2, T3, T4, T5>
        {
            GetAllImpl = GetComponentsFunc(getEntityFunc),
            TryGetForEntityImpl = getEntityFunc
        };
    }

    private Func<IEnumerable<EntityComponents<T1, T2, T3, T4, T5>>> GetComponentsFunc<T1, T2, T3, T4, T5>(Func<EntityId, EntityComponents<T1, T2, T3, T4, T5>?> getEntityFunc)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        return () => _entities.Select(getEntityFunc)
            .Where(x => x is not null)
            .Cast<EntityComponents<T1, T2, T3, T4, T5>>();
    }

    private EntityComponents<T1, T2, T3, T4, T5>? TryGetComponentsForEntity<T1, T2, T3, T4, T5>(EntityId entityId)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        if (_components.TryGetComponent<T1>(entityId, out var component1)
            && _components.TryGetComponent<T2>(entityId, out var component2)
            && _components.TryGetComponent<T3>(entityId, out var component3)
            && _components.TryGetComponent<T4>(entityId, out var component4)
            && _components.TryGetComponent<T5>(entityId, out var component5))
        {
            return new EntityComponents<T1, T2, T3, T4, T5>(entityId)
            {
                Component1 = component1,
                Component2 = component2,
                Component3 = component3,
                Component4 = component4,
                Component5 = component5,
            };
        }

        return null;
    }
}

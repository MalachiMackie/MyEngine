#nullable enable
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Input;
using MyEngine.Physics;
using MyEngine.Rendering;
using MyGame;
using MyGame.Components;
using MyGame.Resources;
using MyGame.Systems;

namespace MyEngine.Runtime;

// todo: source generate this
internal partial class EcsEngine
{
    // resources
    private readonly ResourceContainer _resourceContainer = new();

    // entities
    private readonly HashSet<Core.Ecs.EntityId> _entities = new();

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
    private ToggleColliderDebugDisplaySystem? _toggleColliderDebugDisplaySystem;

    // render systems
    private RenderSystem? _renderSystem;

    // todo: WithoutComponent<T>

    private partial void RunStartupSystems()
    {
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
        {
            if (_resourceContainer.TryGetResource<MyWindow>(out var myWindow)
                && _resourceContainer.TryGetResource<Renderer>(out var renderer))
            {
                new InitializeRenderingSystem(renderer, myWindow).Run();
            }
        }

        {
            if (_resourceContainer.TryGetResource<MyWindow>(out var myWindow)
                && _resourceContainer.TryGetResource<MyInput>(out var myInput))
            {

                new InitializeInputSystem(myWindow, myInput).Run();
            }
        }

    }

    

    public void Update(double dt)
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
        _toggleColliderDebugDisplaySystem?.Run(dt);

        _transformSyncSystem?.Run(dt);

        _renderSystem?.Run(dt);

        AddQueuedResources();
    }

    private partial void AddSystemInstantiations()
    {
        _systemInstantiations.Add(typeof(CameraMovementSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource))
            {
                _cameraMovementSystem = new CameraMovementSystem(
                    inputResource,
                    Query.Create<Camera3DComponent, TransformComponent>(_components, _entities),
                    Query.Create<Camera2DComponent, TransformComponent>(_components, _entities));
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
                    Query.Create<Camera3DComponent, TransformComponent>(_components, _entities),
                    Query.Create<Camera2DComponent, TransformComponent>(_components, _entities),
                    Query.Create<SpriteComponent, TransformComponent>(_components, _entities),
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
                    Query.Create<TransformComponent, StaticBody2DComponent, Collider2DComponent>(_components, _entities),
                    Query.Create(_components, _entities, GetQuery2Components),
                    Query.Create(_components, _entities, GetQuery3Components),
                    Query.Create(_components, _entities, GetQuery4Components));
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
                    Query.Create<BallComponent>(_components, _entities));
                _uninstantiatedSystems.Remove(typeof(ApplyImpulseSystem));
            }
        });

        _systemInstantiations.Add(typeof(RotatePlayerSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                && _resourceContainer.TryGetResource<PhysicsResource>(out var physicsResource))
            {
                _rotatePlayerSystem = new RotatePlayerSystem(
                    Query.Create<BallComponent>(_components, _entities),
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
                _moveBallSystem = new LaunchBallSystem(
                    Query.Create<TransformComponent, BallComponent, KinematicBody2DComponent, ParentComponent>(_components, _entities),
                    inputResource, hierarchyCommands);
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

                _kinematicBounceSystem = new KinematicBounceSystem(
                    Query.Create(_components, _entities, GetComponents),
                    collisionsResource);
                _uninstantiatedSystems.Remove(typeof(KinematicBounceSystem));
            }
        });

        _systemInstantiations.Add(typeof(ResetBallSystem), () =>
        {
            if (_resourceContainer.TryGetResource<WorldSizeResource>(out var worldSizeResource)
                && _resourceContainer.TryGetResource<IHierarchyCommands>(out var hierarchyCommands))
            {
                _resetBallSystem = new ResetBallSystem(Query.Create<TransformComponent, BallComponent, KinematicBody2DComponent>(_components, _entities),
                    worldSizeResource,
                    Query.Create<PaddleComponent, TransformComponent>(_components, _entities),
                    hierarchyCommands);
                _uninstantiatedSystems.Remove(typeof(ResetBallSystem));
            }
        });

        _systemInstantiations.Add(typeof(LogBallPositionSystem), () =>
        {
            _logBallPositionSystem = new LogBallPositionSystem(Query.Create<LogPositionComponent, TransformComponent>(_components, _entities));
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

            _transformSyncSystem = new TransformSyncSystem(Query.Create(_components, _entities, GetQuery1Components));
            _uninstantiatedSystems.Remove(typeof(TransformSyncSystem));
        });

        _systemInstantiations.Add(typeof(MovePaddleSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource))
            {
                _movePaddleSystem = new MovePaddleSystem(Query.Create<TransformComponent, PaddleComponent>(_components, _entities), inputResource);
                _uninstantiatedSystems.Remove(typeof(MovePaddleSystem));
            }
        });

        _systemInstantiations.Add(typeof(ColliderDebugDisplaySystem), () =>
        {
            if (_resourceContainer.TryGetResource<MyPhysics>(out var myPhysics)
                && _resourceContainer.TryGetResource<ILineRenderResource>(out var lineRenderResource)
                && _resourceContainer.TryGetResource<DebugColliderDisplayResource>(out var debugColliderDisplayResource))
            {
                _colliderDebugDisplaySystem = new ColliderDebugDisplaySystem(myPhysics, lineRenderResource, debugColliderDisplayResource);
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
                    Query.Create<BallComponent>(_components, _entities),
                    Query.Create<BrickComponent>(_components, _entities),
                    commands);
                _uninstantiatedSystems.Remove(typeof(BrickCollisionSystem));
            }
        });

        _systemInstantiations.Add(typeof(ToggleColliderDebugDisplaySystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                && _resourceContainer.TryGetResource<DebugColliderDisplayResource>(out var debugColliderDisplayResource))
            {
                _toggleColliderDebugDisplaySystem = new ToggleColliderDebugDisplaySystem(debugColliderDisplayResource, inputResource);
                _uninstantiatedSystems.Remove(typeof(ToggleColliderDebugDisplaySystem));
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
        { typeof(ColliderDebugDisplaySystem), new [] { typeof(MyPhysics), typeof(ILineRenderResource), typeof(DebugColliderDisplayResource) } },
        { typeof(BrickCollisionSystem), new [] { typeof(ICommands), typeof(CollisionsResource) } },
        { typeof(ToggleColliderDebugDisplaySystem), new [] { typeof(InputResource), typeof(DebugColliderDisplayResource) } },
    };

    
}
#nullable restore

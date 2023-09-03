#nullable enable
using MyEngine.Core;
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
    private partial void AddStartupSystemInstantiations()
    {
        _startupSystemInstantiations.Add(typeof(AddCameraStartupSystem), () =>
        {
            if (_resourceContainer.TryGetResource<ICommands>(out var resource1))
            {
                return new AddCameraStartupSystem(resource1);
            }

            return null;
        });

        _startupSystemInstantiations.Add(typeof(AddStartupSpritesSystem), () =>
        {
            if (_resourceContainer.TryGetResource<ICommands>(out var resource1)
                && _resourceContainer.TryGetResource<IHierarchyCommands>(out var resource2)
                && _resourceContainer.TryGetResource<ResourceRegistrationResource>(out var resource3))
            {
                return new AddStartupSpritesSystem(resource1, resource3, resource2);
            }

            return null;
        });

        _startupSystemInstantiations.Add(typeof(InitializeInputSystem), () =>
        {
            if (_resourceContainer.TryGetResource<MyWindow>(out var resource1)
                && _resourceContainer.TryGetResource<MyInput>(out var resource2))
            {
                return new InitializeInputSystem(resource1, resource2);
            }

            return null;
        });

        _startupSystemInstantiations.Add(typeof(InitializeRenderingSystem), () =>
        {
            if (_resourceContainer.TryGetResource<Renderer>(out var resource1)
                && _resourceContainer.TryGetResource<MyWindow>(out var resource2))
            {
                return new InitializeRenderingSystem(resource1, resource2);
            }

            return null;
        });
    }

    private partial void AddSystemInstantiations()
    {
        _systemInstantiations.Add(typeof(CameraMovementSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource))
            {
                return new CameraMovementSystem(
                    inputResource,
                    Query.Create<Camera3DComponent, TransformComponent>(_components, _entities),
                    Query.Create<Camera2DComponent, TransformComponent>(_components, _entities));
            }

            return null;
        });

        _systemInstantiations.Add(typeof(InputSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                && _resourceContainer.TryGetResource<MyInput>(out var myInput))
            {
                return new InputSystem(myInput, inputResource);
            }

            return null;
        });

        _systemInstantiations.Add(typeof(RenderSystem), () =>
        {
            if (_resourceContainer.TryGetResource<Renderer>(out var renderer)
                && _resourceContainer.TryGetResource<ILineRenderResource>(out var lineRenderResource))
            {
                return new RenderSystem(
                    renderer,
                    Query.Create<Camera3DComponent, TransformComponent>(_components, _entities),
                    Query.Create<Camera2DComponent, TransformComponent>(_components, _entities),
                    Query.Create<SpriteComponent, TransformComponent>(_components, _entities),
                    lineRenderResource);
            }

            return null;
        });

        _systemInstantiations.Add(typeof(QuitOnEscapeSystem), () =>
        {
            if (_resourceContainer.TryGetResource<MyWindow>(out var window)
                && _resourceContainer.TryGetResource<InputResource>(out var inputResource))
            {
                return new QuitOnEscapeSystem(window, inputResource);
            }

            return null;
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
                return new PhysicsSystem(
                    physicsResource,
                    collisionsResource,
                    myPhysics,
                    Query.Create<TransformComponent, StaticBody2DComponent, Collider2DComponent>(_components, _entities),
                    Query.Create(_components, _entities, GetQuery2Components),
                    Query.Create(_components, _entities, GetQuery3Components),
                    Query.Create(_components, _entities, GetQuery4Components));
            }

            return null;
        });

        _systemInstantiations.Add(typeof(ApplyImpulseSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                && _resourceContainer.TryGetResource<PhysicsResource>(out var physicsResource))
            {
                return new ApplyImpulseSystem(
                    physicsResource,
                    inputResource,
                    Query.Create<BallComponent>(_components, _entities));
            }

            return null;
        });

        _systemInstantiations.Add(typeof(RotatePlayerSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                && _resourceContainer.TryGetResource<PhysicsResource>(out var physicsResource))
            {
                return new RotatePlayerSystem(
                    Query.Create<BallComponent>(_components, _entities),
                    physicsResource,
                    inputResource);
            }

            return null;
        });

        _systemInstantiations.Add(typeof(OnCollisionSystem), () =>
        {
            if (_resourceContainer.TryGetResource<CollisionsResource>(out var collisionsResource)
                && _resourceContainer.TryGetResource<ICommands>(out var entityContainerResource))
            {
                return new OnCollisionSystem(
                    collisionsResource);
            }

            return null;
        });

        _systemInstantiations.Add(typeof(LaunchBallSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                && _resourceContainer.TryGetResource<IHierarchyCommands>(out var hierarchyCommands))
            {
                return new LaunchBallSystem(
                    Query.Create<TransformComponent, BallComponent, KinematicBody2DComponent, ParentComponent>(_components, _entities),
                    inputResource, hierarchyCommands);
            }

            return null;
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

                return new KinematicBounceSystem(
                    Query.Create(_components, _entities, GetComponents),
                    collisionsResource);
            }

            return null;
        });

        _systemInstantiations.Add(typeof(ResetBallSystem), () =>
        {
            if (_resourceContainer.TryGetResource<WorldSizeResource>(out var worldSizeResource)
                && _resourceContainer.TryGetResource<IHierarchyCommands>(out var hierarchyCommands))
            {
                new ResetBallSystem(Query.Create<TransformComponent, BallComponent, KinematicBody2DComponent>(_components, _entities),
                    worldSizeResource,
                    Query.Create<PaddleComponent, TransformComponent>(_components, _entities),
                    hierarchyCommands);
            }

            return null;
        });

        _systemInstantiations.Add(typeof(LogBallPositionSystem), () =>
        {
            return new LogBallPositionSystem(Query.Create<LogPositionComponent, TransformComponent>(_components, _entities));
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

            return new TransformSyncSystem(Query.Create(_components, _entities, GetQuery1Components));
        });

        _systemInstantiations.Add(typeof(MovePaddleSystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource))
            {
                return new MovePaddleSystem(Query.Create<TransformComponent, PaddleComponent>(_components, _entities), inputResource);
            }

            return null;
        });

        _systemInstantiations.Add(typeof(ColliderDebugDisplaySystem), () =>
        {
            if (_resourceContainer.TryGetResource<MyPhysics>(out var myPhysics)
                && _resourceContainer.TryGetResource<ILineRenderResource>(out var lineRenderResource)
                && _resourceContainer.TryGetResource<DebugColliderDisplayResource>(out var debugColliderDisplayResource))
            {
                return new ColliderDebugDisplaySystem(myPhysics, lineRenderResource, debugColliderDisplayResource);
            }

            return null;
        });

        _systemInstantiations.Add(typeof(BrickCollisionSystem), () =>
        {
            if (_resourceContainer.TryGetResource<ICommands>(out var commands)
                && _resourceContainer.TryGetResource<CollisionsResource>(out var collisionsResource))
            {
                return new BrickCollisionSystem(
                    collisionsResource,
                    Query.Create<BallComponent>(_components, _entities),
                    Query.Create<BrickComponent>(_components, _entities),
                    commands);
            }

            return null;
        });

        _systemInstantiations.Add(typeof(ToggleColliderDebugDisplaySystem), () =>
        {
            if (_resourceContainer.TryGetResource<InputResource>(out var inputResource)
                && _resourceContainer.TryGetResource<DebugColliderDisplayResource>(out var debugColliderDisplayResource))
            {
                return new ToggleColliderDebugDisplaySystem(debugColliderDisplayResource, inputResource);
            }

            return null;
        });
    }

    private readonly IReadOnlyCollection<Type> _allStartupSystemTypes = new []
    {
        typeof(AddStartupSpritesSystem),
        typeof(AddCameraStartupSystem),
        typeof(InitializeInputSystem),
        typeof(InitializeRenderingSystem)
    };


    private readonly IReadOnlyCollection<Type> _allSystemTypes = new []
    {
        typeof(CameraMovementSystem),
        typeof(InputSystem),
        typeof(RenderSystem),
        typeof(QuitOnEscapeSystem),
        typeof(PhysicsSystem),
        typeof(ApplyImpulseSystem),
        typeof(RotatePlayerSystem),
        typeof(OnCollisionSystem),
        typeof(LaunchBallSystem),
        typeof(KinematicBounceSystem),
        typeof(ResetBallSystem),
        typeof(LogBallPositionSystem),
        typeof(TransformSyncSystem),
        typeof(MovePaddleSystem),
        typeof(ColliderDebugDisplaySystem),
        typeof(BrickCollisionSystem),
        typeof(ToggleColliderDebugDisplaySystem),
    };

    private readonly Dictionary<Type, Type[]> _uninstantiatedStartupSystems = new()
    {
        { typeof(AddCameraStartupSystem), new [] { typeof(ICommands) } },
        { typeof(AddStartupSpritesSystem), new [] { typeof(ICommands), typeof(IHierarchyCommands), typeof(ResourceRegistrationResource) } },
        { typeof(InitializeInputSystem), new [] { typeof(MyWindow), typeof(MyInput) } },
        { typeof(InitializeRenderingSystem), new [] { typeof(MyWindow), typeof(Renderer) } }
    };

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

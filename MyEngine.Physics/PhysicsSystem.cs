using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Physics;

public class PhysicsSystem : ISystem
{
    private readonly PhysicsResource _physicsResource;
    private readonly CollisionsResource _collisionsResource;
    private readonly IPhysicsAdapter _physicsAdapter;
    private readonly IQuery<TransformComponent, StaticBody2DComponent, Collider2DComponent> _staticBodiesQuery;
    private readonly IQuery<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<ParentComponent>, OptionalComponent<VelocityComponent>> _dynamicBodiesQuery;
    private readonly IQuery<TransformComponent, KinematicBody2DComponent, Collider2DComponent, OptionalComponent<ParentComponent>> _kinematicBodiesQuery;
    private readonly IQuery<TransformComponent, OptionalComponent<ParentComponent>> _transformAndParentQuery;

    public PhysicsSystem(PhysicsResource physicsResource,
        CollisionsResource collisionsResource,
        IPhysicsAdapter physicsAdapter,
        IQuery<TransformComponent, StaticBody2DComponent, Collider2DComponent> staticBodiesQuery,
        IQuery<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<ParentComponent>, OptionalComponent<VelocityComponent>> dynamicBodiesQuery,
        IQuery<TransformComponent, KinematicBody2DComponent, Collider2DComponent, OptionalComponent<ParentComponent>> kinematicBodiesQuery,
        IQuery<TransformComponent, OptionalComponent<ParentComponent>> transformAndParentQuery)
    {
        _physicsResource = physicsResource;
        _collisionsResource = collisionsResource;
        _physicsAdapter = physicsAdapter;
        _staticBodiesQuery = staticBodiesQuery;
        _dynamicBodiesQuery = dynamicBodiesQuery;
        _kinematicBodiesQuery = kinematicBodiesQuery;
        _transformAndParentQuery = transformAndParentQuery;
    }

    // todo: split into systems: writeToPhysics, physicsStep, writeBackPhysics
    public void Run(double deltaTime)
    {
        var staticBodies = _physicsAdapter.GetStaticBodies();
        var dynamicBodies = _physicsAdapter.GetDynamicBodies();

        var extraStaticBodies = new HashSet<EntityId>(staticBodies);
        var extraDynamicBodies = new HashSet<EntityId>(dynamicBodies);
        var transformsToGetUpdatesFor = new Dictionary<EntityId, PhysicsWriteBack>();
        var dynamicTransformsToUpdate = new Dictionary<EntityId, GlobalTransform>();
        var staticTransformsToUpdate = new Dictionary<EntityId, GlobalTransform>();

        foreach (var components in _staticBodiesQuery)
        {
            var (transform, _, collider) = components;

            if (!extraStaticBodies.Remove(components.EntityId))
            {
                // this is a new static body
                _physicsResource.AddStaticBody2D(components.EntityId, transform.GlobalTransform, collider.Collider);
            }

            staticTransformsToUpdate.Add(components.EntityId, transform.GlobalTransform);
        }
        foreach (var components in _dynamicBodiesQuery)
        {
            var (transform, _, collider, parent, velocity) = components;

            if (!extraDynamicBodies.Remove(components.EntityId))
            {
                // this is a new dynamic body
                _physicsResource.AddDynamicBody2D(components.EntityId, transform.GlobalTransform, collider.Collider);
            }

            GlobalTransform? parentTransform = null;
            if (parent.HasComponent)
            {
                parentTransform = _transformAndParentQuery.TryGetForEntity(parent.Component.Parent)!.Component1.GlobalTransform;
            }

            transformsToGetUpdatesFor.Add(components.EntityId, new PhysicsWriteBack(components.EntityId, transform, parentTransform, velocity?.Component));
            dynamicTransformsToUpdate.Add(components.EntityId, transform.GlobalTransform);
        }

        foreach (var components in _kinematicBodiesQuery)
        {
            var (transform, _, collider, parent) = components;
            // todo: set velocity from velocity component

            var globalTransform = transform.GlobalTransform;
            if (!extraDynamicBodies.Remove(components.EntityId))
            {
                // this is a new kinematic body
                _physicsResource.AddKinematicBody2D(components.EntityId, globalTransform, collider.Collider);
            }
            GlobalTransform? parentTransform = null;
            if (parent.HasComponent)
            {
                parentTransform = _transformAndParentQuery.TryGetForEntity(parent.Component.Parent)!.Component1.GlobalTransform;
            }

            transformsToGetUpdatesFor.Add(components.EntityId, new PhysicsWriteBack(components.EntityId, transform, parentTransform, /*todo: velocity component for kinematic body*/null));
            dynamicTransformsToUpdate.Add(components.EntityId, globalTransform);
        }

        foreach (var extraStaticBody in extraStaticBodies)
        {
            _physicsResource.RemoveStaticBody(extraStaticBody);
            staticTransformsToUpdate.Remove(extraStaticBody);
        }

        foreach (var extraDynamicBody in extraDynamicBodies)
        {
            _physicsResource.RemoveDynamicBody(extraDynamicBody);
            dynamicTransformsToUpdate.Remove(extraDynamicBody);
            transformsToGetUpdatesFor.Remove(extraDynamicBody);
        }

        foreach (var (entityId, transform) in dynamicTransformsToUpdate)
        {
            _physicsResource.SetDynamicTransform(entityId, transform);
        }

        foreach (var (entityId, transform) in staticTransformsToUpdate)
        {
            _physicsResource.SetStaticTransform(entityId, transform);
        }
        
        _physicsResource.Update(deltaTime);

        UpdateTransformsAfterPhysicsUpdate(transformsToGetUpdatesFor.Values);

        ProcessCommands();
    }

    private void ProcessCommands()
    {
        while (_physicsResource.PhysicsCommands.TryDequeue(out var cmd))
        {
            switch (cmd)
            {
                case PhysicsResource.ApplyImpulseCommand applyImpulse:
                    _physicsAdapter.ApplyImpulse(applyImpulse.entityId, applyImpulse.impulse);
                    break;
                case PhysicsResource.ApplyAngularImpulseCommand applyAngularImpulse:
                    _physicsAdapter.ApplyAngularImpulse(applyAngularImpulse.entityId, applyAngularImpulse.impulse);
                    break;
                case PhysicsResource.SetDynamicTransformCommand updateDynamicTransform:
                    {
                        var result = _physicsAdapter.ApplyDynamicPhysicsTransform(updateDynamicTransform.entityId, updateDynamicTransform.transform);
                        if (result.TryGetErrors(out var error))
                        {
                            Console.WriteLine("Failed to apply dynamic physics transform: {0}", string.Join(',', error));
                        }
                        break;
                    }
                case PhysicsResource.SetStaticTransformCommand updateStaticTransform:
                    {
                        var result = _physicsAdapter.ApplyStaticPhysicsTransform(updateStaticTransform.entityId, updateStaticTransform.transform);
                        if (result.TryGetErrors(out var error))
                        {
                            Console.WriteLine("Failed to set static transform: {0}", string.Join(';', error));
                        }
                        break;
                    }
                case PhysicsResource.PhysicsWriteBackCommand updateTransformFromPhysics:
                    {
                        var entityTransform = updateTransformFromPhysics.transform;
                        var (physicsPosition, physicsRotation, physicsVelocity) = _physicsAdapter.GetDynamicPhysicsInfo(updateTransformFromPhysics.entityId);

                        entityTransform.GlobalTransform.SetComponents(physicsPosition, physicsRotation, entityTransform.GlobalTransform.Scale);

                        Vector3 localTranslation;
                        Quaternion localRotation;

                        // now immediately sync global transform back to the local transform so that it doesn't immediately get set back by the transform sync system
                        if (updateTransformFromPhysics.parentTransform is not null)
                        {
                            if (!Matrix4x4.Invert(updateTransformFromPhysics.parentTransform.ModelMatrix, out var inverseParent))
                            {
                                Console.WriteLine("Failed to write physics transform back to engine transform");
                                break;
                            }

                            var localMatrix = entityTransform.GlobalTransform.ModelMatrix * inverseParent;

                            if (!Matrix4x4.Decompose(localMatrix, out _, out localRotation, out localTranslation))
                            {
                                Console.WriteLine("Failed to write physics transform back to engine transform");
                                break;
                            }
                        }
                        else
                        {
                            if (!Matrix4x4.Decompose(entityTransform.GlobalTransform.ModelMatrix, out _, out localRotation, out localTranslation))
                            {
                                Console.WriteLine("Failed to write physics transform back to engine transform");
                                break;
                            }
                        }


                        entityTransform.LocalTransform.position = localTranslation;
                        entityTransform.LocalTransform.rotation = localRotation;
                        if (updateTransformFromPhysics.velocity is not null)
                        {
                            updateTransformFromPhysics.velocity.Velocity = physicsVelocity;
                        }

                        break;
                    }
                case PhysicsResource.AddStaticBodyCommand addStaticBody:
                    {
                        var result = _physicsAdapter.AddStaticBody(addStaticBody.entityId, addStaticBody.transform);
                        if (result.TryGetErrors(out var error))
                        {
                            // todo: add centralized logging
                            Console.WriteLine("Failed to add static body: {0}", string.Join(';', error));
                        }
                        break;
                    }
                case PhysicsResource.AddStaticBody2DCommand addStaticBody2D:
                    {
                        var result = _physicsAdapter.AddStaticBody2D(addStaticBody2D.entityId, addStaticBody2D.transform, addStaticBody2D.collider);
                        if (result.TryGetErrors(out var error))
                        {
                            Console.WriteLine("Failed to add static body 2D: {0}", string.Join(";", error));
                        }
                        break;
                    }
                case PhysicsResource.AddDynamicBodyCommand addDynamicBody:
                    {
                        var result = _physicsAdapter.AddDynamicBody(addDynamicBody.entityId, addDynamicBody.transform);
                        if (result.TryGetErrors(out var error))
                        {
                            Console.WriteLine("Failed to add dynamic body: {0}", string.Join(", ", error));
                        }
                        break;
                    }
                case PhysicsResource.AddDynamicBody2DCommand addDynamicBody2D:
                    {
                        var result = _physicsAdapter.AddDynamicBody2D(addDynamicBody2D.entityId, addDynamicBody2D.transform, addDynamicBody2D.collider);
                        if (result.TryGetErrors(out var error))
                        {
                            Console.WriteLine("Failed to add dynamic body 2D: {0}", string.Join(", ", error));
                        }
                        break;
                    }
                case PhysicsResource.AddKinematicBody2DCommand addKinematicBody2D:
                    {
                        var result = _physicsAdapter.AddKinematicBody2D(addKinematicBody2D.entityId, addKinematicBody2D.transform, addKinematicBody2D.collider);
                        if (result.TryGetErrors(out var error))
                        {
                            Console.WriteLine("Failed to add kinematic body 2D: {0}", string.Join(", ", error));
                        }
                    }
                    break;
                case PhysicsResource.RemoveStaticBodyCommand removeStaticBody:
                    _physicsAdapter.RemoveStaticBody(removeStaticBody.entityId);
                    break;
                case PhysicsResource.RemoveDynamicBodyCommand removeDynamicBody:
                    _physicsAdapter.RemoveDynamicBody(removeDynamicBody.entityId);
                    break;
                case PhysicsResource.SetBody2DVelocityCommand setKinematicBody2DVelocityCommand:
                    _physicsAdapter.SetDynamicBody2DVelocity(setKinematicBody2DVelocityCommand.entityId, setKinematicBody2DVelocityCommand.velocity);
                    break;
                case PhysicsResource.UpdateCommand update:
                    {
                        _collisionsResource._newCollisions.Clear();
                        _collisionsResource._existingCollisions.Clear();
                        _collisionsResource._oldCollisions.Clear();

                        _physicsAdapter.Update(update.dt, out var newCollisions, out var continuingCollisions, out var oldCollisions);

                        _collisionsResource._newCollisions.AddRange(newCollisions);
                        _collisionsResource._existingCollisions.AddRange(continuingCollisions);
                        _collisionsResource._oldCollisions.AddRange(oldCollisions);
                        break;
                    }
            }
        }
    }

    private record struct PhysicsWriteBack(
        EntityId EntityId,
        TransformComponent TransformComponent,
        GlobalTransform? ParentTransform,
        VelocityComponent? Velocity); 

    private void UpdateTransformsAfterPhysicsUpdate(IEnumerable<PhysicsWriteBack> dynamicTransforms)
    {
        foreach (var (entityId, transformComponent, parentTransform, velocity) in dynamicTransforms)
        {
            _physicsResource.PhysicsWriteBack(entityId, transformComponent, parentTransform, velocity);
        }
    }
}

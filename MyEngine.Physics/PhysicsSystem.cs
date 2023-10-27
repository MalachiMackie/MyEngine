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
    private readonly MyPhysics _myPhysics;
    private readonly IQuery<TransformComponent, StaticBody2DComponent, Collider2DComponent> _staticBodiesQuery;
    private readonly IQuery<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>, OptionalComponent<ParentComponent>> _dynamicBodiesQuery;
    private readonly IQuery<TransformComponent, KinematicBody2DComponent, Collider2DComponent, OptionalComponent<ParentComponent>> _kinematicBodiesQuery;
    private readonly IQuery<TransformComponent, OptionalComponent<ParentComponent>> _transformAndParentQuery;

    public PhysicsSystem(PhysicsResource physicsResource,
        CollisionsResource collisionsResource,
        MyPhysics myPhysics,
        IQuery<TransformComponent, StaticBody2DComponent, Collider2DComponent> staticBodiesQuery,
        IQuery<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>, OptionalComponent<ParentComponent>> dynamicBodiesQuery,
        IQuery<TransformComponent, KinematicBody2DComponent, Collider2DComponent, OptionalComponent<ParentComponent>> kinematicBodiesQuery,
        IQuery<TransformComponent, OptionalComponent<ParentComponent>> transformAndParentQuery)
    {
        _physicsResource = physicsResource;
        _collisionsResource = collisionsResource;
        _myPhysics = myPhysics;
        _staticBodiesQuery = staticBodiesQuery;
        _dynamicBodiesQuery = dynamicBodiesQuery;
        _kinematicBodiesQuery = kinematicBodiesQuery;
        _transformAndParentQuery = transformAndParentQuery;
    }

    // todo: split into systems: writeToPhysics, physicsStep, writeBackPhysics
    public void Run(double deltaTime)
    {
        var staticBodies = _myPhysics.GetStaticBodies();
        var dynamicBodies = _myPhysics.GetDynamicBodies();

        var extraStaticBodies = new HashSet<EntityId>(staticBodies);
        var extraDynamicBodies = new HashSet<EntityId>(dynamicBodies);
        var transformsToGetUpdatesFor = new Dictionary<EntityId, TransformWriteBack>();
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
            var (transform, _, collider, material, parent) = components;

            if (!extraDynamicBodies.Remove(components.EntityId))
            {
                // this is a new dynamic body
                _physicsResource.AddDynamicBody2D(components.EntityId, transform.GlobalTransform, collider.Collider, material.Component?.Bounciness ?? 0f);
            }

            GlobalTransform? parentTransform = null;
            if (parent.HasComponent)
            {
                parentTransform = _transformAndParentQuery.TryGetForEntity(parent.Component.Parent)!.Component1.GlobalTransform;
            }

            transformsToGetUpdatesFor.Add(components.EntityId, new TransformWriteBack(components.EntityId, transform, parentTransform));
            dynamicTransformsToUpdate.Add(components.EntityId, transform.GlobalTransform);
        }

        foreach (var components in _kinematicBodiesQuery)
        {
            var (transform, kinematicBody, collider, parent) = components;

            var globalTransform = transform.GlobalTransform;
            if (!extraDynamicBodies.Remove(components.EntityId))
            {
                // this is a new kinematic body
                _physicsResource.AddKinematicBody2D(components.EntityId, globalTransform, collider.Collider);
            }
            else if (kinematicBody.Dirty)
            {
                _physicsResource.SetKinematicBody2DVelocity(components.EntityId, kinematicBody.Velocity);
                kinematicBody.Dirty = false;
            }
            GlobalTransform? parentTransform = null;
            if (parent.HasComponent)
            {
                parentTransform = _transformAndParentQuery.TryGetForEntity(parent.Component.Parent)!.Component1.GlobalTransform;
            }

            transformsToGetUpdatesFor.Add(components.EntityId, new TransformWriteBack(components.EntityId, transform, parentTransform));
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
                    _myPhysics.ApplyImpulse(applyImpulse.entityId, applyImpulse.impulse);
                    break;
                case PhysicsResource.ApplyAngularImpulseCommand applyAngularImpulse:
                    _myPhysics.ApplyAngularImpulse(applyAngularImpulse.entityId, applyAngularImpulse.impulse);
                    break;
                case PhysicsResource.SetDynamicTransformCommand updateDynamicTransform:
                    {
                        var result = _myPhysics.ApplyDynamicPhysicsTransform(updateDynamicTransform.entityId, updateDynamicTransform.transform);
                        if (result.TryGetError(out var error))
                        {
                            Console.WriteLine("Failed to apply dynamic physics transform: {0}", error.Error.Error);
                        }
                        break;
                    }
                case PhysicsResource.SetStaticTransformCommand updateStaticTransform:
                    {
                        var result = _myPhysics.ApplyStaticPhysicsTransform(updateStaticTransform.entityId, updateStaticTransform.transform);
                        if (result.TryGetError(out var error))
                        {
                            Console.WriteLine("Failed to set static transform: {0}", error.Error.Error);
                        }
                        break;
                    }
                case PhysicsResource.UpdateTransformFromPhysicsCommand updateTransformFromPhysics:
                    {
                        var entityTransform = updateTransformFromPhysics.transform;
                        var (physicsPosition, physicsRotation) = _myPhysics.GetDynamicPhysicsTransform(updateTransformFromPhysics.entityId);

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

                        break;
                    }
                case PhysicsResource.AddStaticBodyCommand addStaticBody:
                    {
                        var result = _myPhysics.AddStaticBody(addStaticBody.entityId, addStaticBody.transform);
                        if (result.TryGetError(out var error))
                        {
                            Console.WriteLine("Failed to add static body: {0}", error.Error.Error);
                        }
                        break;
                    }
                case PhysicsResource.AddStaticBody2DCommand addStaticBody2D:
                    {
                        var result = _myPhysics.AddStaticBody2D(addStaticBody2D.entityId, addStaticBody2D.transform, addStaticBody2D.collider);
                        if (result.TryGetError(out var error))
                        {
                            Console.WriteLine("Failed to add static body 2D: {0}", error.Error.Error);
                        }
                        break;
                    }
                case PhysicsResource.AddDynamicBodyCommand addDynamicBody:
                    {
                        var result = _myPhysics.AddDynamicBody(addDynamicBody.entityId, addDynamicBody.transform, addDynamicBody.bounciness);
                        if (result.TryGetError(out var error))
                        {
                            Console.WriteLine("Failed to add dynamic body: {0}", error.Error.Error);
                        }
                        break;
                    }
                case PhysicsResource.AddDynamicBody2DCommand addDynamicBody2D:
                    {
                        var result = _myPhysics.AddDynamicBody2D(addDynamicBody2D.entityId, addDynamicBody2D.transform, addDynamicBody2D.collider, addDynamicBody2D.bounciness);
                        if (result.TryGetError(out var error))
                        {
                            Console.WriteLine("Failed to add dynamic body 2D: {0}", error.Error.Error);
                        }
                        break;
                    }
                case PhysicsResource.AddKinematicBody2DCommand addKinematicBody2D:
                    {
                        var result = _myPhysics.AddKinematicBody2D(addKinematicBody2D.entityId, addKinematicBody2D.transform, addKinematicBody2D.collider);
                        if (result.TryGetError(out var error))
                        {
                            Console.WriteLine("Failed to add kinematic body 2D: {0}", error.Error.Error);
                        }
                    }
                    break;
                case PhysicsResource.RemoveStaticBodyCommand removeStaticBody:
                    _myPhysics.RemoveStaticBody(removeStaticBody.entityId);
                    break;
                case PhysicsResource.RemoveDynamicBodyCommand removeDynamicBody:
                    _myPhysics.RemoveDynamicBody(removeDynamicBody.entityId);
                    break;
                case PhysicsResource.SetKinematicBody2DVelocityCommand setKinematicBody2DVelocityCommand:
                    _myPhysics.SetDynamicBody2DVelocity(setKinematicBody2DVelocityCommand.entityId, setKinematicBody2DVelocityCommand.velocity);
                    break;
                case PhysicsResource.UpdateCommand update:
                    {
                        _collisionsResource._newCollisions.Clear();
                        _collisionsResource._existingCollisions.Clear();
                        _collisionsResource._oldCollisions.Clear();

                        _myPhysics.Update(update.dt, out var newCollisions, out var continuingCollisions, out var oldCollisions);

                        _collisionsResource._newCollisions.AddRange(newCollisions);
                        _collisionsResource._existingCollisions.AddRange(continuingCollisions);
                        _collisionsResource._oldCollisions.AddRange(oldCollisions);
                        break;
                    }
            }
        }
    }

    private record struct TransformWriteBack(EntityId EntityId, TransformComponent TransformComponent, GlobalTransform? parentTransform); 

    private void UpdateTransformsAfterPhysicsUpdate(IEnumerable<TransformWriteBack> dynamicTransforms)
    {
        foreach (var (entityId, transformComponent, parentTransform) in dynamicTransforms)
        {
            _physicsResource.UpdateTransformFromPhysics(entityId, transformComponent, parentTransform);
        }
    }
}

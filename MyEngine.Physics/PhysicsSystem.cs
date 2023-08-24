using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
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
    private readonly IQuery<TransformComponent> _transformQuery;

    public PhysicsSystem(PhysicsResource physicsResource,
        CollisionsResource collisionsResource,
        MyPhysics myPhysics,
        IQuery<TransformComponent, StaticBody2DComponent, Collider2DComponent> staticBodiesQuery,
        IQuery<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>, OptionalComponent<ParentComponent>> dynamicBodiesQuery,
        IQuery<TransformComponent, KinematicBody2DComponent, Collider2DComponent, OptionalComponent<ParentComponent>> kinematicBodiesQuery,
        IQuery<TransformComponent> transformQuery)
    {
        _physicsResource = physicsResource;
        _collisionsResource = collisionsResource;
        _myPhysics = myPhysics;
        _staticBodiesQuery = staticBodiesQuery;
        _dynamicBodiesQuery = dynamicBodiesQuery;
        _kinematicBodiesQuery = kinematicBodiesQuery;
        _transformQuery = transformQuery;
    }

    // todo: split into systems: writeToPhysics, physicsStep, writeBackPhysics
    public void Run(double deltaTime)
    {
        var staticBodies = _myPhysics.GetStaticBodies();
        var dynamicBodies = _myPhysics.GetDynamicBodies();

        var extraStaticBodies = new HashSet<EntityId>(staticBodies);
        var extraDynamicBodies = new HashSet<EntityId>(dynamicBodies);
        var transformsToGetUpdatesFor = new Dictionary<EntityId, TransformWriteBack>();
        var dynamicTransformsToUpdate = new Dictionary<EntityId, TransformComponent>();
        var staticTransformsToUpdate = new Dictionary<EntityId, TransformComponent>();

        foreach (var components in _staticBodiesQuery)
        {
            var (transform, staticBody, collider) = components;
            if (!extraStaticBodies.Remove(components.EntityId))
            {
                // this is a new static body
                _physicsResource.AddStaticBody2D(components.EntityId, transform.GlobalTransform, collider.Collider);
            }

            staticTransformsToUpdate.Add(components.EntityId, transform);
        }
        foreach (var components in _dynamicBodiesQuery)
        {
            var (transform, dynamicBody, collider, material, parent) = components;
            if (!extraDynamicBodies.Remove(components.EntityId))
            {
                // this is a new dynamic body
                _physicsResource.AddDynamicBody2D(components.EntityId, transform.GlobalTransform, collider.Collider, material.Component?.Bounciness ?? 0f);
            }

            GlobalTransform? parentTransform = null;
            if (parent.HasComponent)
            {
                parentTransform = _transformQuery.TryGetForEntity(parent.Component.Parent)!.Component.GlobalTransform;
            }

            transformsToGetUpdatesFor.Add(components.EntityId, new TransformWriteBack(components.EntityId, transform, parentTransform));
            dynamicTransformsToUpdate.Add(components.EntityId, transform);
        }
        foreach (var components in _kinematicBodiesQuery)
        {
            var (transform, kinematicBody, collider, parent) = components;
            if (!extraDynamicBodies.Remove(components.EntityId))
            {
                // this is a new kinematic body
                _physicsResource.AddKinematicBody2D(components.EntityId, transform.GlobalTransform, collider.Collider);
            }
            else if (kinematicBody.Dirty)
            {
                _physicsResource.SetKinematicBody2DVelocity(components.EntityId, kinematicBody.Velocity);
                kinematicBody.Dirty = false;
            }

            GlobalTransform? parentTransform = null;

            if (parent.HasComponent)
            {
                parentTransform = _transformQuery.TryGetForEntity(parent.Component.Parent)!.Component.GlobalTransform;
            }

            transformsToGetUpdatesFor.Add(components.EntityId, new TransformWriteBack(components.EntityId, transform, parentTransform));
            dynamicTransformsToUpdate.Add(components.EntityId, transform);
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
            _physicsResource.SetDynamicTransform(entityId, transform.GlobalTransform);
        }

        foreach (var (entityId, transform) in staticTransformsToUpdate)
        {
            _physicsResource.SetStaticTransform(entityId, transform.GlobalTransform);
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
                    _myPhysics.ApplyDynamicPhysicsTransform(updateDynamicTransform.entityId, updateDynamicTransform.transform);
                    break;
                case PhysicsResource.SetStaticTransformCommand updateStaticTransform:
                    _myPhysics.ApplyStaticPhysicsTransform(updateStaticTransform.entityId, updateStaticTransform.transform);
                    break;
                case PhysicsResource.UpdateTransformFromPhysicsCommand updateTransformFromPhysics:
                    {
                        var entityTransform = updateTransformFromPhysics.transform;
                        var physicsGlobalTransform = _myPhysics.GetDynamicPhysicsTransform(updateTransformFromPhysics.entityId);

                        if (updateTransformFromPhysics.parentTransform is not null)
                        {
                            // we have a parent transform, so we need to convert our physicsGlobalTransform to a local transform
                            var parentTransform = updateTransformFromPhysics.parentTransform;
                            var positionDifference = parentTransform.position * -1;
                            var rotationDifference = Quaternion.Inverse(parentTransform.rotation);

                            entityTransform.position = physicsGlobalTransform.position + positionDifference;
                            entityTransform.rotation = rotationDifference * physicsGlobalTransform.rotation;
                        }
                        else
                        {
                            // no parent, just write the physicsGlobalTransform to the transform
                            entityTransform.position = physicsGlobalTransform.position;
                            entityTransform.rotation = physicsGlobalTransform.rotation;
                        }

                        break;
                    }
                case PhysicsResource.AddStaticBodyCommand addStaticBody:
                    _myPhysics.AddStaticBody(addStaticBody.entityId, addStaticBody.transform);
                    break;
                case PhysicsResource.AddStaticBody2DCommand addStaticBody2D:
                    _myPhysics.AddStaticBody2D(addStaticBody2D.entityId, addStaticBody2D.transform, addStaticBody2D.collider);
                    break;
                case PhysicsResource.AddDynamicBodyCommand addDynamicBody:
                    _myPhysics.AddDynamicBody(addDynamicBody.entityId, addDynamicBody.transform, addDynamicBody.bounciness);
                    break;
                case PhysicsResource.AddDynamicBody2DCommand addDynamicBody2D:
                    _myPhysics.AddDynamicBody2D(addDynamicBody2D.entityId, addDynamicBody2D.transform, addDynamicBody2D.collider, addDynamicBody2D.bounciness);
                    break;
                case PhysicsResource.AddKinematicBody2DCommand addKinematicBody2D:
                    _myPhysics.AddKinematicBody2D(addKinematicBody2D.entityId, addKinematicBody2D.transform, addKinematicBody2D.collider);
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
                        // todo: continuing collisions and old collisions
                        _myPhysics.Update(update.dt, out var newCollisions, out _, out _);
                        _collisionsResource._newCollisions.AddRange(newCollisions);
                        break;
                    }
            }
        }
    }

    private record struct TransformWriteBack(EntityId EntityId, TransformComponent TransformComponent, GlobalTransform? ParentTransform); 

    private void UpdateTransformsAfterPhysicsUpdate(IEnumerable<TransformWriteBack> dynamicTransforms)
    {
        foreach (var (entityId, transformComponent, parentTransform) in dynamicTransforms)
        {
            _physicsResource.UpdateTransformFromPhysics(entityId, transformComponent.LocalTransform, parentTransform);
        }
    }
}

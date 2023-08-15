﻿using MyEngine.Core;
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
    private readonly IEnumerable<EntityComponents<TransformComponent, StaticBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>>> _staticBodiesQuery;
    private readonly IEnumerable<EntityComponents<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>>> _dynamicBodiesQuery;

    public PhysicsSystem(PhysicsResource physicsResource,
        CollisionsResource collisionsResource,
        MyPhysics myPhysics,
        IEnumerable<EntityComponents<TransformComponent, StaticBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>>> staticBodiesQuery,
        IEnumerable<EntityComponents<TransformComponent, DynamicBody2DComponent, Collider2DComponent, OptionalComponent<PhysicsMaterial>>> dynamicBodiesQuery)
    {
        _physicsResource = physicsResource;
        _collisionsResource = collisionsResource;
        _myPhysics = myPhysics;
        _staticBodiesQuery = staticBodiesQuery;
        _dynamicBodiesQuery = dynamicBodiesQuery;
    }

    public void Run(double deltaTime)
    {
        // todo: get static bodies and dynamic bodies from the physics engine, rather than trying to remember them
        var staticBodies = _myPhysics.GetStaticBodies();
        var dynamicBodies = _myPhysics.GetDynamicBodies();

        var extraStaticBodies = new HashSet<EntityId>(staticBodies);
        var extraDynamicBodies = new HashSet<EntityId>(dynamicBodies);
        var dynamicTransformsToUpdate = new Dictionary<EntityId, Transform>();

        foreach (var components in _staticBodiesQuery)
        {
            var (transform, staticBody, collider, material) = components;
            if (!extraStaticBodies.Remove(components.EntityId))
            {
                // this is a new static body
                _physicsResource.AddStaticBody2D(components.EntityId, new Transform
                {
                    position = transform.Transform.position,
                    rotation = transform.Transform.rotation,
                    scale = transform.Transform.scale,
                }, collider.Collider, material.Component?.Bounciness ?? 0f);
            }
        }
        foreach (var components in _dynamicBodiesQuery)
        {
            var (transform, dynamicBody, collider, material) = components;
            if (!extraDynamicBodies.Remove(components.EntityId))
            {
                // this is a new dynamic body
                _physicsResource.AddDynamicBody2D(components.EntityId, new Transform
                {
                    position = transform.Transform.position,
                    rotation = transform.Transform.rotation,
                    scale = transform.Transform.scale,
                }, collider.Collider, material.Component?.Bounciness ?? 0f);
            }

            dynamicTransformsToUpdate.Add(components.EntityId, transform.Transform);
        }

        foreach (var extraStaticBody in extraStaticBodies)
        {
            _physicsResource.RemoveStaticBody(extraStaticBody);
        }

        foreach (var extraDynamicBody in extraDynamicBodies)
        {
            _physicsResource.RemoveStaticBody(extraDynamicBody);
            dynamicTransformsToUpdate.Remove(extraDynamicBody);
        }
        
        _physicsResource.Update(deltaTime);

        UpdateTransforms(dynamicTransformsToUpdate.Select(x => (x.Key, x.Value)));

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
                case PhysicsResource.UpdateDynamicTransformCommand updateDynamicTransform:
                    _myPhysics.UpdateDynamicTransform(updateDynamicTransform.entityId, updateDynamicTransform.transform);
                    break;
                case PhysicsResource.AddStaticBodyCommand addStaticBody:
                    _myPhysics.AddStaticBody(addStaticBody.entityId, addStaticBody.transform, addStaticBody.bounciness);
                    break;
                case PhysicsResource.AddStaticBody2DCommand addStaticBody2D:
                    _myPhysics.AddStaticBody2D(addStaticBody2D.entityId, addStaticBody2D.transform, addStaticBody2D.collider, addStaticBody2D.bounciness);
                    break;
                case PhysicsResource.AddDynamicBodyCommand addDynamicBody:
                    _myPhysics.AddDynamicBody(addDynamicBody.entityId, addDynamicBody.transform, addDynamicBody.bounciness);
                    break;
                case PhysicsResource.AddDynamicBody2DCommand addDynamicBody2D:
                    _myPhysics.AddDynamicBody2D(addDynamicBody2D.entityId, addDynamicBody2D.transform, addDynamicBody2D.collider, addDynamicBody2D.bounciness);
                    break;
                case PhysicsResource.RemoveStaticBodyCommand removeStaticBody:
                    _myPhysics.RemoveStaticBody(removeStaticBody.entityId);
                    break;
                case PhysicsResource.RemoveDynamicBodyCommand removeDynamicBody:
                    _myPhysics.RemoveDynamicBody(removeDynamicBody.entityId);
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

    private void UpdateTransforms(IEnumerable<(EntityId, Transform)> dynamicTransforms)
    {
        foreach (var (entityId, transform) in dynamicTransforms)
        {
            _physicsResource.UpdateDynamicTransform(entityId, transform);
        }
    }
}

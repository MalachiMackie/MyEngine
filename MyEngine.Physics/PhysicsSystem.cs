using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using System.Numerics;

namespace MyEngine.Physics
{
    public class PhysicsSystem : ISystem
    {
        private readonly PhysicsResource _physicsResource;
        private readonly CollisionsResource _collisionsResource;
        private readonly MyPhysics _myPhysics;
        private readonly MyQuery<TransformComponent, StaticBody2DComponent, BoxCollider2DComponent, OptionalComponent<PhysicsMaterial>> _staticBodiesQuery;
        private readonly MyQuery<TransformComponent, DynamicBody2DComponent, BoxCollider2DComponent, OptionalComponent<PhysicsMaterial>> _dynamicBodiesQuery;

        public PhysicsSystem(PhysicsResource physicsResource,
            CollisionsResource collisionsResource,
            MyPhysics myPhysics,
            MyQuery<TransformComponent, StaticBody2DComponent, BoxCollider2DComponent, OptionalComponent<PhysicsMaterial>> staticBodiesQuery,
            MyQuery<TransformComponent, DynamicBody2DComponent, BoxCollider2DComponent, OptionalComponent<PhysicsMaterial>> dynamicBodiesQuery)
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
            var staticTransformsToUpdate = new Dictionary<EntityId, Transform>();
            var dynamicTransformsToUpdate = new Dictionary<EntityId, Transform>();

            foreach (var (transform, staticBody, collider, material) in _staticBodiesQuery)
            {
                if (!extraStaticBodies.Remove(staticBody.EntityId))
                {
                    // this is a new static body
                    var scale = transform.Transform.scale * new Vector3(collider.Dimensions, 1f);

                    _physicsResource.AddStaticBody2D(transform.EntityId, new Transform
                    {
                        position = transform.Transform.position,
                        rotation = transform.Transform.rotation,
                        scale = scale,
                    }, material.Component?.Bounciness ?? 0f);
                }

                staticTransformsToUpdate.Add(transform.EntityId, transform.Transform);
            }
            foreach (var (transform, dynamicBody, collider, material) in _dynamicBodiesQuery)
            {
                if (!extraDynamicBodies.Remove(dynamicBody.EntityId))
                {
                    // this is a new dynamic body
                    var scale = transform.Transform.scale * new Vector3(collider.Dimensions, 1f);

                    _physicsResource.AddDynamicBody2D(transform.EntityId, new Transform
                    {
                        position = transform.Transform.position,
                        rotation = transform.Transform.rotation,
                        scale = scale,
                    }, material.Component?.Bounciness ?? 0f);
                }

                dynamicTransformsToUpdate.Add(transform.EntityId, transform.Transform);
            }

            foreach (var extraStaticBody in extraStaticBodies)
            {
                _physicsResource.RemoveStaticBody(extraStaticBody);
                staticTransformsToUpdate.Remove(extraStaticBody);
            }

            foreach (var extraDynamicBody in extraDynamicBodies)
            {
                _physicsResource.RemoveStaticBody(extraDynamicBody);
                dynamicTransformsToUpdate.Remove(extraDynamicBody);
            }
            
            _physicsResource.Update(deltaTime);

            UpdateTransforms(staticTransformsToUpdate.Select(x => (x.Key, x.Value)), dynamicTransformsToUpdate.Select(x => (x.Key, x.Value)));

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
                    case PhysicsResource.UpdateStaticTransformCommand updateStaticTransform:
                        _myPhysics.UpdateStaticTransform(updateStaticTransform.entityId, updateStaticTransform.transform);
                        break;
                    case PhysicsResource.UpdateDynamicTransformCommand updateDynamicTransform:
                        _myPhysics.UpdateDynamicTransform(updateDynamicTransform.entityId, updateDynamicTransform.transform);
                        break;
                    case PhysicsResource.AddStaticBodyCommand addStaticBody:
                        _myPhysics.AddStaticBody(addStaticBody.entityId, addStaticBody.transform, addStaticBody.bounciness);
                        break;
                    case PhysicsResource.AddStaticBody2DCommand addStaticBody2D:
                        _myPhysics.AddStaticBody2D(addStaticBody2D.entityId, addStaticBody2D.transform, addStaticBody2D.bounciness);
                        break;
                    case PhysicsResource.AddDynamicBodyCommand addDynamicBody:
                        _myPhysics.AddDynamicBody(addDynamicBody.entityId, addDynamicBody.transform, addDynamicBody.bounciness);
                        break;
                    case PhysicsResource.AddDynamicBody2DCommand addDynamicBody2D:
                        _myPhysics.AddDynamicBody2D(addDynamicBody2D.entityId, addDynamicBody2D.transform, addDynamicBody2D.bounciness);
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
                            _myPhysics.Update(update.dt, out var newCollisions);
                            _collisionsResource._newCollisions.AddRange(newCollisions);
                            break;
                        }
                }
            }
        }

        private void UpdateTransforms(IEnumerable<(EntityId, Transform)> staticTransforms, IEnumerable<(EntityId, Transform)> dynamicTransforms)
        {
            foreach (var (entityId, transform) in staticTransforms)
            {
                _physicsResource.UpdateStaticTransform(entityId, transform);
            }
            foreach (var (entityId, transform) in dynamicTransforms)
            {
                _physicsResource.UpdateDynamicTransform(entityId, transform);
            }
        }
    }
}

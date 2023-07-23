using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.Physics
{
    public class PhysicsSystem : ISystem
    {
        private readonly PhysicsResource _physicsResource;
        private readonly MyQuery<TransformComponent, StaticBody2DComponent> _staticBodiesQuery;
        private readonly MyQuery<TransformComponent, DynamicBody2DComponent> _dynamicBodiesQuery;

        private readonly HashSet<EntityId> _staticBodies = new();
        private readonly HashSet<EntityId> _dynamicBodies = new();

        public PhysicsSystem(PhysicsResource physicsResource,
            MyQuery<TransformComponent, StaticBody2DComponent> staticBodiesQuery,
            MyQuery<TransformComponent, DynamicBody2DComponent> dynamicBodiesQuery)
        {
            _physicsResource = physicsResource;
            _staticBodiesQuery = staticBodiesQuery;
            _dynamicBodiesQuery = dynamicBodiesQuery;
        }

        public void Run(double deltaTime)
        {
            var extraStaticBodies = new HashSet<EntityId>(_staticBodies);
            var extraDynamicBodies = new HashSet<EntityId>(_dynamicBodies);
            var staticTransformsToUpdate = new Dictionary<EntityId, Transform>();
            var dynamicTransformsToUpdate = new Dictionary<EntityId, Transform>();

            foreach (var (transform, staticBody) in _staticBodiesQuery)
            {
                if (!extraStaticBodies.Remove(staticBody.EntityId))
                {
                    // this is a new static body
                    _physicsResource.AddStaticBody(transform.EntityId, transform.Transform);
                    _staticBodies.Add(transform.EntityId);
                }

                staticTransformsToUpdate.Add(transform.EntityId, transform.Transform);
            }
            foreach (var (transform, dynamicBody) in _dynamicBodiesQuery)
            {
                if (!extraDynamicBodies.Remove(dynamicBody.EntityId))
                {
                    // this is a new dynamic body
                    _physicsResource.AddDynamicBody(transform.EntityId, transform.Transform);
                    _dynamicBodies.Add(transform.EntityId);
                }

                dynamicTransformsToUpdate.Add(transform.EntityId, transform.Transform);
            }

            foreach (var extraStaticBody in extraStaticBodies)
            {
                _physicsResource.RemoveStaticBody(extraStaticBody);
                _dynamicBodies.Remove(extraStaticBody);
                staticTransformsToUpdate.Remove(extraStaticBody);
            }

            foreach (var extraDynamicBody in extraDynamicBodies)
            {
                _physicsResource.RemoveStaticBody(extraDynamicBody);
                _dynamicBodies.Remove(extraDynamicBody);
                dynamicTransformsToUpdate.Remove(extraDynamicBody);
            }
            
            _physicsResource.Update(deltaTime);

            UpdateTransforms(staticTransformsToUpdate.Select(x => (x.Key, x.Value)), dynamicTransformsToUpdate.Select(x => (x.Key, x.Value)));
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

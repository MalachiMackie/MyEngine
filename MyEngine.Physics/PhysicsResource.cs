using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.Physics
{
    public class PhysicsResource : IResource
    {
        private readonly MyPhysics _myPhysics;

        public PhysicsResource()
        {
            _myPhysics = new MyPhysics();
        }

        internal void Update(double dt)
        {
            _myPhysics.Update(dt);
        }

        public void RemoveStaticBody(EntityId entityId)
        {
            _myPhysics.RemoveStaticBody(entityId);
        }

        public void AddStaticBody(EntityId entityId, Transform transform)
        {
            _myPhysics.AddStaticBody(entityId, transform);
        }

        public void RemoveDynamicBody(EntityId entityId)
        {
            _myPhysics.RemoveDynamicBody(entityId);
        }

        public void AddDynamicBody(EntityId entityId, Transform transform)
        {
            _myPhysics.AddDynamicBody(entityId, transform);
        }

        public void UpdateStaticTransform(EntityId entityId, Transform transform)
        {
            _myPhysics.UpdateStaticTransform(entityId, transform);
        }

        public void UpdateDynamicTransform(EntityId entityId, Transform transform)
        {
            _myPhysics.UpdateDynamicTransform(entityId, transform);
        }
    }
}

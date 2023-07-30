using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.Core.Ecs.Components
{
    public class PhysicsMaterial : IComponent
    {
        public EntityId EntityId { get; }

        public float Bounciness { get; set; }

        public PhysicsMaterial(EntityId entityId, float bounciness)
        {
            EntityId = entityId;
            Bounciness = Math.Clamp(bounciness, 0f, 1f);
        }

    }
}

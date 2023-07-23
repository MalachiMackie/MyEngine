using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.Core.Ecs.Components
{
    public class DynamicBody2DComponent : IComponent
    {
        public EntityId EntityId { get; }
        public Vector2 Size { get; }

        public DynamicBody2DComponent(EntityId entityId, Vector2 size)
        {
            EntityId = entityId;
            Size = size;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.Core.Ecs.Components
{
    public class StaticBody2DComponent : IComponent
    {
        public StaticBody2DComponent(
            EntityId entityId,
            Vector2 size)
        {
            EntityId = entityId;
            Size = size;
        }

        public EntityId EntityId { get; }

        public Vector2 Size { get; }

    }
}

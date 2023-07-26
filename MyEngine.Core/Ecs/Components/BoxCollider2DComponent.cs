using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.Core.Ecs.Components
{
    public class BoxCollider2DComponent : IComponent
    {
        public EntityId EntityId { get; }

        public Vector2 Dimensions { get; }

        public BoxCollider2DComponent(EntityId entityId,
            Vector2 dimensions)
        {
            EntityId = entityId;
            Dimensions = dimensions;
        }
    }
}

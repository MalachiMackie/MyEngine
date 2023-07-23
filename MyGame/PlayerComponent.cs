using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGame
{
    internal class PlayerComponent : IComponent
    {
        public EntityId EntityId { get; }

        public PlayerComponent(EntityId entityId)
        {
            EntityId = entityId;
        }
    }
}

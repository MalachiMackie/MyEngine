using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGame
{
    public class TestComponent : IComponent
    {
        public EntityId EntityId { get; }

        public TestComponent(EntityId entityId)
        {
            EntityId = entityId;
        }
    }
}

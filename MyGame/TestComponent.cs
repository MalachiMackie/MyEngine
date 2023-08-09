using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;

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

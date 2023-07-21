using System.Numerics;

namespace MyEngine.Core.Ecs.Components
{
    public class CameraComponent : IComponent
    {
        public static bool AllowMultiple => false;

        public CameraComponent(EntityId entityId)
        {
            EntityId = entityId;
        }

        public EntityId EntityId { get; }
    }
}

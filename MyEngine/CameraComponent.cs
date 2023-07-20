using System.Numerics;

namespace MyEngine
{
    internal class CameraComponent : IComponent
    {
        public static bool AllowMultiple => false;

        public CameraComponent(EntityId entityId)
        {
            EntityId = entityId;
        }

        public EntityId EntityId { get; }

        public Vector3 CameraFront { get; set; }
    }
}

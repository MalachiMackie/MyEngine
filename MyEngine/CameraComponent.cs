using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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

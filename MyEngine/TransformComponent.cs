using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine
{
    internal class TransformComponent : IComponent
    {
        public TransformComponent(EntityId entityId) 
        {
            EntityId = entityId;
            Transform = new Transform()
            {
                scale = Vector3.One,
                position = new Vector3(0.0f, 0.0f, 3.0f),
                rotation = Quaternion.CreateFromYawPitchRoll(0.0f, 0f, -90f)
            };
        }

        public EntityId EntityId { get; }

        public Transform Transform { get; }

        public static bool AllowMultiple => false;
    }
}

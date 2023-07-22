using System.Numerics;

namespace MyEngine.Core.Ecs.Components
{
    public class TransformComponent : IComponent
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

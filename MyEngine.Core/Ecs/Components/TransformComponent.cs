namespace MyEngine.Core.Ecs.Components
{
    public class TransformComponent : IComponent
    {
        public TransformComponent()
        {
            Transform = new Transform()
            {
                scale = Vector3.One,
                position = new Vector3(0.0f, 0.0f, 3.0f),
                rotation = Quaternion.CreateFromYawPitchRoll(0.0f, 0f, -90f)
            };
        }

        public TransformComponent(Transform transform)
        {
            Transform = transform;
        }

        public Transform Transform { get; }

        public static bool AllowMultiple => false;
    }
}

namespace MyEngine.Core.Ecs.Components;

public class TransformComponent : IComponent
{
    public static Transform DefaultTransform(Vector3? position = null, Vector3? scale = null, Quaternion? rotation = null)
    {
        return new Transform
        {
            position = position ?? new Vector3(0.0f, 0.0f, 3.0f),
            scale = scale ?? Vector3.One,
            rotation = rotation ?? Quaternion.CreateFromYawPitchRoll(0.0f, 0f, -90f)
        };
    }

    public TransformComponent(Transform transform)
    {
        Transform = transform;
    }

    public TransformComponent(Vector3? position = null, Vector3? scale = null, Quaternion? rotation = null)
    {
        Transform = DefaultTransform(position, scale, rotation);
    }

    public Transform Transform { get; }
}

namespace MyEngine.Core;

public class Transform
{
    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
    }

    public Transform()
    {

    }

    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public static Transform Default(Vector3? position = null, Vector3? scale = null, Quaternion? rotation = null)
    {
        return new Transform(
            position ?? new Vector3(0.0f, 0.0f, 3.0f),
            rotation ?? Quaternion.CreateFromYawPitchRoll(0.0f, 0f, -90f),
            scale ?? Vector3.One);
    }

}

public class GlobalTransform
{
    public GlobalTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
    }

    public Vector3 position { get; private set; }
    public Quaternion rotation { get; private set; }
    public Vector3 scale { get; private set; }

    public Matrix4x4 ViewMatrix => Matrix4x4.Identity * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(position);

    internal void SyncWithLocalTransform(GlobalTransform? parentTransform, Transform localTransform)
    {
        if (parentTransform is null)
        {
            position = localTransform.position;
            rotation = localTransform.rotation;
            scale = localTransform.scale;
            return;
        }

        position = parentTransform.position + localTransform.position;
        rotation = parentTransform.rotation * localTransform.rotation;
        scale = parentTransform.scale * localTransform.scale;
    }

    public static GlobalTransform Default(Vector3? position = null, Vector3? scale = null, Quaternion? rotation = null)
    {
        return new GlobalTransform(position ?? new Vector3(0.0f, 0.0f, 3.0f), rotation ?? Quaternion.CreateFromYawPitchRoll(0.0f, 0f, -90f), scale ?? Vector3.One);
    }

    public static GlobalTransform FromTransform(Transform transform)
    {
        return new GlobalTransform(transform.position, transform.rotation, transform.scale);
    }
}

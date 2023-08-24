namespace MyEngine.Core;

public struct Transform
{
    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
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
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public Matrix4x4 ViewMatrix => Matrix4x4.Identity * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(position);

    public static GlobalTransform Default(Vector3? position = null, Vector3? scale = null, Quaternion? rotation = null)
    {
        return new GlobalTransform
        {
            position = position ?? new Vector3(0.0f, 0.0f, 3.0f),
            scale = scale ?? Vector3.One,
            rotation = rotation ?? Quaternion.CreateFromYawPitchRoll(0.0f, 0f, -90f)
        };
    }

    public static GlobalTransform FromTransform(Transform transform)
    {
        return new GlobalTransform
        {
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.scale
        };
    }
}

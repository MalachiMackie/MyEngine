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
            rotation ?? Quaternion.CreateFromYawPitchRoll(0.0f, 0f, 0f),
            scale ?? Vector3.One);
    }

}

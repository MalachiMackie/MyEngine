using MyEngine.Utils;

namespace MyEngine.Core;

public class GlobalTransform : IEquatable<GlobalTransform>
{
    public record struct PositionRotationScale(Vector3 Position, Quaternion Rotation, Vector3 Scale);

    public GlobalTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        ModelMatrix = CreateMatrix(position, rotation, scale);
        Scale = scale;
    }

    public Matrix4x4 ModelMatrix { get; private set; }

    // keep scale separately because it doesn't get affected by physics, but we need it in order to make the ModelMatrix.
    // rather than trying to decompose it out of the existing matrix (which can fail), we just keep it around to use
    public Vector3 Scale { get; private set; }
    public Vector3 Position => ModelMatrix.Translation;

    public Result<PositionRotationScale> GetPositionRotationScale()
    {
        if (!Matrix4x4.Decompose(ModelMatrix, out var _, out var rotation, out var translation))
        {
            var matrix = ModelMatrix;
            MathHelper.NormalizeMatrix(ref matrix);

            if (!Matrix4x4.Decompose(ModelMatrix, out _, out rotation, out translation))
            {
                return Result.Failure<PositionRotationScale>(
                    "Could not extract Position, Rotation and Scale from Model Matrix. This is likely because the transform has both rotation and non-uniform scaling");
            }
        }

        return Result.Success<PositionRotationScale>(new(translation, rotation, Scale));
    }


    private static Matrix4x4 CreateMatrix(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return Matrix4x4.CreateScale(scale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position)
           ;
    }

    internal void SetWithTransform(Transform transform)
    {
        ModelMatrix = CreateMatrix(transform.position, transform.rotation, transform.scale);
        Scale = transform.scale;
    }

    internal void SetComponents(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        ModelMatrix = CreateMatrix(position, rotation, scale);
    }

    public static GlobalTransform Default(Vector3? position = null, Vector3? scale = null, Quaternion? rotation = null)
    {
        return new GlobalTransform(position ?? new Vector3(0.0f, 0.0f, 3.0f), rotation ?? Quaternion.CreateFromYawPitchRoll(0.0f, 0f, 0f), scale ?? Vector3.One);
    }

    public static GlobalTransform FromTransform(Transform transform)
    {
        return new GlobalTransform(transform.position, transform.rotation, transform.scale);
    }

    internal void SetWithParentTransform(GlobalTransform parentTransform, Transform localTransform)
    {
        var localMatrix = CreateMatrix(localTransform.position, localTransform.rotation, localTransform.scale);

        ModelMatrix = localMatrix * parentTransform.ModelMatrix;
        Scale = parentTransform.Scale * localTransform.scale;
    }

    public bool Equals(GlobalTransform? other)
    {
        return ModelMatrix == other?.ModelMatrix;
    }
}

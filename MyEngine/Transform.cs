using System.Numerics;

namespace MyEngine
{
    internal class Transform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public Matrix4x4 ViewMatrix => Matrix4x4.Identity * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(position);
    }
}

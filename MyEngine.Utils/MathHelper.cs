using System.Numerics;

namespace MyEngine.Utils;

public static class MathHelper
{
    public static float DegreesToRadians(float degrees)
    {
        return MathF.PI / 180f * degrees;
    }

    public static float RadiansToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }

    public static Vector3 ToEulerAngles(this Quaternion q)
    {
        Vector3 angles = new();

        // roll / x
        double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.X = RadiansToDegrees((float)Math.Atan2(sinr_cosp, cosr_cosp));

        // pitch / y
        double sinp = 2 * (q.W * q.Y - q.Z * q.X);
        if (Math.Abs(sinp) >= 1)
        {
            angles.Y = RadiansToDegrees((float)Math.CopySign(Math.PI / 2, sinp));
        }
        else
        {
            angles.Y = RadiansToDegrees((float)Math.Asin(sinp));
        }

        // yaw / z
        double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.Z = RadiansToDegrees((float)Math.Atan2(siny_cosp, cosy_cosp));

        return angles;
    }

    public static Quaternion ToQuaternion(this Vector3 v)
    {
        v = new Vector3(DegreesToRadians(v.X), DegreesToRadians(v.Y), DegreesToRadians(v.Z));

        var cy = MathF.Cos(v.Z * 0.5f);
        var sy = MathF.Sin(v.Z * 0.5f);
        var cp = MathF.Cos(v.Y * 0.5f);
        var sp = MathF.Sin(v.Y * 0.5f);
        var cr = MathF.Cos(v.X * 0.5f);
        var sr = MathF.Sin(v.X * 0.5f);

        return new Quaternion
        {
            W = cr * cp * cy + sr * sp * sy,
            X = sr * cp * cy - cr * sp * sy,
            Y = cr * sp * cy + sr * cp * sy,
            Z = cr * cp * sy - sr * sp * cy
        };
    }

    public static Vector2 XY(this Vector3 v)
    {
        return new Vector2(v.X, v.Y);
    }
}

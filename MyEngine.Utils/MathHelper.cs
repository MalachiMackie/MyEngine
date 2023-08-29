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

    public static Vector3 Extend(this Vector2 v, float z)
    {
        return new Vector3(v, z);
    }

    public static Vector2 Normalize(this Vector2 v)
    {
        return Vector2.Normalize(v);
    }

    public enum Vector2WithMagnitudeError
    {
        MagnitudeLessThan0
    }

    public static Result<Vector2, Vector2WithMagnitudeError> WithMagnitude(this Vector2 v, float magnitude)
    {
        if (magnitude < 0)
        {
            return Result.Failure<Vector2, Vector2WithMagnitudeError>(Vector2WithMagnitudeError.MagnitudeLessThan0);
        }

        if (magnitude < 0.00001f)
        {
            // setting magnitude to zero removes all data 
            return Result.Success<Vector2, Vector2WithMagnitudeError>(Vector2.Zero);
        }

        if (v.Length() < 0.00001f)
        {
            // if current magnitude is zero, then we just keep it at zero 
            return Result.Success<Vector2, Vector2WithMagnitudeError>(Vector2.Zero);
        }

        return Result.Success<Vector2, Vector2WithMagnitudeError>(v.Normalize() * magnitude);
    }

    // https://gist.github.com/vpenades/9e6248bf8558aa1d802885c2ab984e14 
    public static void NormalizeMatrix(ref Matrix4x4 xform)
    {
        var vx = new Vector3(xform.M11, xform.M12, xform.M13);
        var vy = new Vector3(xform.M21, xform.M22, xform.M23);
        var vz = new Vector3(xform.M31, xform.M32, xform.M33);

        var lx = vx.Length();
        var ly = vy.Length();
        var lz = vz.Length();

        // normalize axis vectors
        vx /= lx;
        vy /= ly;
        vz /= lz;

        // determine the skew of each axis (the smaller, the more orthogonal the axis is)
        var kxy = Math.Abs(Vector3.Dot(vx, vy));
        var kxz = Math.Abs(Vector3.Dot(vx, vz));
        var kyz = Math.Abs(Vector3.Dot(vy, vz));

        var kx = kxy + kxz;
        var ky = kxy + kyz;
        var kz = kxz + kyz;

        // we will use the axis with less skew as our fixed pivot.

        // axis X as pivot
        if (kx < ky && kx < kz)
        {
            if (ky < kz)
            {
                vz = Vector3.Cross(vx, vy);
                vy = Vector3.Cross(vz, vx);
            }
            else
            {
                vy = Vector3.Cross(vz, vx);
                vz = Vector3.Cross(vx, vy);
            }
        }

        // axis Y as pivot
        else if (ky < kx && ky < kz)
        {
            if (kx < kz)
            {
                vz = Vector3.Cross(vx, vy);
                vx = Vector3.Cross(vy, vz);
            }
            else
            {
                vx = Vector3.Cross(vy, vz);
                vz = Vector3.Cross(vx, vy);
            }
        }

        // axis z as pivot
        else
        {
            if (kx < ky)
            {
                vy = Vector3.Cross(vz, vx);
                vx = Vector3.Cross(vy, vz);
            }
            else
            {
                vx = Vector3.Cross(vy, vz);
                vy = Vector3.Cross(vz, vx);
            }
        }

        // restore axes original lengths
        vx *= lx;
        vy *= ly;
        vz *= lz;

        xform.M11 = vx.X;
        xform.M12 = vx.Y;
        xform.M13 = vx.Z;

        xform.M21 = vy.X;
        xform.M22 = vy.Y;
        xform.M23 = vy.Z;

        xform.M31 = vz.X;
        xform.M32 = vz.Y;
        xform.M33 = vz.Z;
    }
}

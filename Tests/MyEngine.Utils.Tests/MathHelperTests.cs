using System.Collections;
using System.Numerics;
using FluentAssertions;

namespace MyEngine.Utils.Tests;
public class MathHelperTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, MathF.PI / 2f)]
    [InlineData(180, MathF.PI)]
    [InlineData(-180, -MathF.PI)]
    [InlineData(270, MathF.PI * 1.5f)]
    [InlineData(360, MathF.PI * 2f)]
    [InlineData(540, MathF.PI * 3f)]
    public void DegreesToRadians_Should_ReturnExpected(float degrees, float expectedRadians)
    {
        var radians = MathHelper.DegreesToRadians(degrees);

        radians.Should().BeApproximately(expectedRadians, 0.000001f);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(MathF.PI / 2f, 90)]
    [InlineData(MathF.PI, 180)]
    [InlineData(-MathF.PI, -180)]
    [InlineData(MathF.PI * 1.5f, 270)]
    [InlineData(MathF.PI * 2f, 360)]
    [InlineData(MathF.PI * 3f, 540)]
    public void RadiansToDegrees_Should_ReturnExpected(float radians, float expectedDegrees)
    {
        var degrees = MathHelper.RadiansToDegrees(radians);

        degrees.Should().BeApproximately(expectedDegrees, 0.000001f);
    }

    [Theory]
    [ClassData(typeof(QuaternionAndEulerDegreesTestCases))]
    public void QuaternionToEulerDegrees_Should_ReturnExpected(Quaternion quaternion, Vector3 expectedEulerAngles)
    {
        var eulerAngles = quaternion.ToEulerAngles();

        const float tollerence = 0.02f;

        eulerAngles.X.Should().BeApproximately(expectedEulerAngles.X, tollerence);
        eulerAngles.Y.Should().BeApproximately(expectedEulerAngles.Y, tollerence);
        eulerAngles.Z.Should().BeApproximately(expectedEulerAngles.Z, tollerence);
    }

    [Theory]
    [ClassData(typeof(QuaternionAndEulerDegreesTestCases))]
    public void EulerAnglesToQuaternion_Should_ReturnExpected(Quaternion expectedQuaternion, Vector3 eulerAngles)
    {
        var quaternion = eulerAngles.ToQuaternion();

        const float tollerence = 0.02f;
        quaternion.X.Should().BeApproximately(expectedQuaternion.X, tollerence);
        quaternion.Y.Should().BeApproximately(expectedQuaternion.Y, tollerence);
        quaternion.Z.Should().BeApproximately(expectedQuaternion.Z, tollerence);
        quaternion.W.Should().BeApproximately(expectedQuaternion.W, tollerence);
    }

    [Fact]
    public void Vector3XY_Should_ReturnXY()
    {
        var vector3 = new Vector3(1f, 2f, 3f);
        var xy = vector3.XY();

        xy.Should().Be(new Vector2(1f, 2f));
    }

    [Fact]
    public void Vector2Extend1_Should_ReturnVector3()
    {
        var vector2 = new Vector2(1f, 2f);
        var extended = vector2.Extend(3f);

        extended.Should().Be(new Vector3(1f, 2f, 3f));
    }

    [Fact]
    public void Vector2Extend2_Should_ReturnVector4()
    {
        var vector2 = new Vector2(1f, 2f);
        var extended = vector2.Extend(3f, 4f);

        extended.Should().Be(new Vector4(1f, 2f, 3f, 4f));
    }

    [Fact]
    public void Vector3Extend_Should_ReturnVector4()
    {
        var vector3 = new Vector3(1f, 2f, 3f);
        var extended = vector3.Extend(4f);

        extended.Should().Be(new Vector4(1f, 2f, 3f, 4f));
    }

    [Fact]
    public void Vector2Normalize_Should_NormalizeVector()
    {
        var vector = new Vector2(1f, 3f);
        var normalized = vector.Normalize();

        normalized.Should().Be(Vector2.Normalize(vector));
    }

    [Fact]
    public void Vector3Normalize_Should_NormalizeVector()
    {
        var vector = new Vector3(1f, 3f, 5f);
        var normalized = vector.Normalize();

        normalized.Should().Be(Vector3.Normalize(vector));
    }

    [Fact]
    public void Vector2WithMagnitude_Should_CorrectlySetMagnitude()
    {
        var vector = new Vector2(3f, 0f);
        var result = vector.WithMagnitude(2f);

        result.IsSuccess.Should().BeTrue();

        result.Unwrap().Should().Be(new Vector2(2f, 0f));
    }

    [Fact]
    public void Vector2WithMagnitude_Should_ReturnZero_When_MagnitudeIsZero()
    {
        var result = new Vector2(3f, 0f).WithMagnitude(0f);

        result.IsSuccess.Should().BeTrue();

        result.Unwrap().Should().Be(Vector2.Zero);
    }

    [Fact]
    public void Vector2WithMagnitude_Should_ReturnZero_When_LengthIsZero()
    {
        var result = new Vector2(0f, 0f).WithMagnitude(3f);

        result.IsSuccess.Should().BeTrue();
        result.Unwrap().Should().Be(Vector2.Zero);
    }

    [Fact]
    public void Vector2WithMagnitude_Should_ReturnFailure_When_MagnitudeIsNegative()
    {
        var result = new Vector2(0f, 0f).WithMagnitude(-1f);
        result.IsFailure.Should().BeTrue();
        result.UnwrapError().Should().Be(MathHelper.VectorWithMagnitudeError.MagnitudeLessThan0);
    }

    [Fact]
    public void Vector3WithMagnitude_Should_CorrectlySetMagnitude()
    {
        var vector = new Vector3(3f, 0f, 0f);
        var result = vector.WithMagnitude(2f);

        result.IsSuccess.Should().BeTrue();

        result.Unwrap().Should().Be(new Vector3(2f, 0f, 0f));
    }

    [Fact]
    public void Vector3WithMagnitude_Should_ReturnZero_When_MagnitudeIsZero()
    {
        var result = new Vector3(3f, 0f, 0f).WithMagnitude(0f);

        result.IsSuccess.Should().BeTrue();

        result.Unwrap().Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Vector3WithMagnitude_Should_ReturnZero_When_LengthIsZero()
    {
        var result = new Vector3(0f, 0f, 0f).WithMagnitude(3f);

        result.IsSuccess.Should().BeTrue();
        result.Unwrap().Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Vector3WithMagnitude_Should_ReturnFailure_When_MagnitudeIsNegative()
    {
        var result = new Vector3(0f, 0f, 0f).WithMagnitude(-1f);
        result.IsFailure.Should().BeTrue();
        result.UnwrapError().Should().Be(MathHelper.VectorWithMagnitudeError.MagnitudeLessThan0);
    }

    [Fact]
    public void NormalizeMatrix_Should_ReturnCorrectNormalizedMatrix()
    {
        static void testSkewed(Func<Matrix4x4, Matrix4x4> modifyMatrix)
        {
            var matrix = Matrix4x4.Identity;

            var original = matrix = modifyMatrix(matrix);
            
            MathHelper.NormalizeMatrix(ref matrix);

            const float tollerence = 0.34f;

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    matrix[i, j].Should().BeApproximately(original[i, j], tollerence);
                }
            }


            Matrix4x4.Decompose(matrix, out _, out _, out _).Should().BeTrue();
            Matrix4x4.Invert(matrix, out _).Should().BeTrue();               
        }
        
        testSkewed(m => { m.M12 += 0.34f; return m; });
        testSkewed(m => { m.M13 += 0.34f; return m; });
        testSkewed(m => { m.M21 += 0.34f; return m; });
        testSkewed(m => { m.M23 += 0.34f; return m; });
        testSkewed(m => { m.M31 += 0.34f; return m; });
        testSkewed(m => { m.M32 += 0.34f; return m; });

        testSkewed(m => { m.M12 += 0.1f; m.M23 -= 0.1f; m.M31 += 0.05f; return m; });

        // test normalization with uneven scaling

        var SxR = Matrix4x4.CreateScale(5, 1, 1) * Matrix4x4.CreateFromYawPitchRoll(1, 2, 3);   // Decomposable
        var RxS = Matrix4x4.CreateFromYawPitchRoll(1, 2, 3) * Matrix4x4.CreateScale(5, 1, 1);   // Not Decomposable            

        Matrix4x4.Decompose(SxR, out _, out _, out _).Should().BeTrue();
        Matrix4x4.Decompose(RxS, out _, out _, out _).Should().BeFalse();
        
        testSkewed(m => SxR);
    }
}

file class QuaternionAndEulerDegreesTestCases : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { Quaternion.Identity, Vector3.Zero };
        yield return new object[] { Quaternion.CreateFromYawPitchRoll(MathF.PI * 0.5f, 0f, 0f), new Vector3(0f, 90f, 0f) };
        yield return new object[] { Quaternion.CreateFromYawPitchRoll(-MathF.PI * 0.5f, 0f, 0f), new Vector3(0f, -90f, 0f) };
        yield return new object[] { Quaternion.CreateFromYawPitchRoll(0f, MathF.PI * 0.5f, 0f), new Vector3(90f, 0f, 0f) };
        yield return new object[] { Quaternion.CreateFromYawPitchRoll(0f, 0f, MathF.PI * 0.5f), new Vector3(0f, 0f, 90f) };
        yield return new object[] {
            Quaternion.CreateFromYawPitchRoll(MathF.PI * 0.5f, 0f, MathF.PI * 0.5f),
            new Vector3(90f, 0f, 90f)
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

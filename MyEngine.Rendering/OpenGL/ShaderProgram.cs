using MyEngine.Utils;
using Silk.NET.OpenGL;
using System.Numerics;

namespace MyEngine.Rendering.OpenGL;

internal class ShaderProgram : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;

    private ShaderProgram(GL gl, uint handle)
    {
        _gl = gl;
        _handle = handle;
    }

    public void SetUniform1(string uniformName, int value)
    {
        var location = _gl.GetUniformLocation(_handle, uniformName);
        _gl.Uniform1(location, value);
    }

    public unsafe void SetUniform1(string uniformName, Matrix4x4 matrix)
    {
        var location = _gl.GetUniformLocation(_handle, uniformName);
        // there is an overload that takes a Span<float>,
        // which would enable this method to be safe, but there's no way to safely get long lasting span of a matrix
        _gl.UniformMatrix4(location, 1, false, (float*)&matrix);
    }

    public void UseProgram()
    {
        _gl.UseProgram(_handle);
    }

    public void Dispose()
    {
        _gl.DeleteProgram(_handle);
    }

    private static Result<uint> LoadShader(GL gl, ShaderType shaderType, string source)
    {
        var handle = gl.CreateShader(shaderType);
        gl.ShaderSource(handle, source);
        gl.CompileShader(handle);

        gl.GetShader(handle, ShaderParameterName.CompileStatus, out var status);
        if (status != (int)GLEnum.True)
        {
            return Result.Failure<uint>($"{shaderType} shader failed to compile: {gl.GetShaderInfoLog(handle)}");
        }

        return Result.Success<uint>(handle);
    }

    public static Result<ShaderProgram> Create(GL gl, string vertexSource, string fragmentSource)
    {

        var vertexResult = LoadShader(gl, ShaderType.VertexShader, vertexSource);

        if (!vertexResult.TryGetValue(out var vertexHandle))
        {
            return Result.Failure<ShaderProgram, uint>(vertexResult);
        }

        var fragmentResult = LoadShader(gl, ShaderType.FragmentShader, fragmentSource);

        if (!fragmentResult.TryGetValue(out var fragmentHandle))
        {
            return Result.Failure<ShaderProgram, uint>(fragmentResult);
        }

        var handle = gl.CreateProgram();

        var shader = new ShaderProgram(gl, handle);

        gl.AttachShader(handle, vertexHandle);
        gl.AttachShader(handle, fragmentHandle);
        gl.LinkProgram(handle);

        gl.GetProgram(handle, ProgramPropertyARB.LinkStatus, out var lStatus);
        if (lStatus != (int)GLEnum.True)
        {
            return Result.Failure<ShaderProgram>($"Faled to link shader program: {gl.GetProgramInfoLog(handle)}");
        }

        gl.DetachShader(handle, vertexHandle);
        gl.DetachShader(handle, fragmentHandle);
        gl.DeleteShader(vertexHandle);
        gl.DeleteShader(fragmentHandle);

        return Result.Success<ShaderProgram>(shader);

    }
}

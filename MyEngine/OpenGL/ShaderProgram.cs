using MyEngine.Utils;
using Silk.NET.OpenGL;
using System.Numerics;

using CreateShaderProgramError = MyEngine.Utils.OneOf<
    MyEngine.Runtime.OpenGL.VertexShaderCompilationFailed,
    MyEngine.Runtime.OpenGL.FragmentShaderCompilationFailed,
    MyEngine.Runtime.OpenGL.ShaderProgramLinkFailed>;

namespace MyEngine.Runtime.OpenGL;

internal readonly record struct VertexShaderCompilationFailed(string CompilationError);
internal readonly record struct FragmentShaderCompilationFailed(string CompilationError);
internal readonly record struct ShaderProgramLinkFailed(string LinkError);

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

    private static Result<uint, string> LoadShader(GL gl, ShaderType shaderType, string source)
    {
        var handle = gl.CreateShader(shaderType);
        gl.ShaderSource(handle, source);
        gl.CompileShader(handle);

        gl.GetShader(handle, ShaderParameterName.CompileStatus, out int status);
        if (status != (int)GLEnum.True)
        {
            return Result.Failure<uint, string>($"{shaderType} shader failed to compile: {gl.GetShaderInfoLog(handle)}");
        }

        return Result.Success<uint, string>(handle);
    }

    public static Result<ShaderProgram, CreateShaderProgramError> Create(GL gl, string vertexSource, string fragmentSource)
    {

        var vertexResult = LoadShader(gl, ShaderType.VertexShader, vertexSource)
            .MapError(err => new CreateShaderProgramError(new VertexShaderCompilationFailed(err)));

        if (!vertexResult.TryGetValue(out var vertexHandle))
        {
            return Result.Failure<ShaderProgram, CreateShaderProgramError>(vertexResult.UnwrapError());
        }

        var fragmentResult = LoadShader(gl, ShaderType.FragmentShader, fragmentSource)
            .MapError(err => new CreateShaderProgramError(new FragmentShaderCompilationFailed(err)));

        if (!fragmentResult.TryGetValue(out var fragmentHandle))
        {
            return Result.Failure<ShaderProgram, CreateShaderProgramError>(fragmentResult.UnwrapError());
        }
        
        var handle = gl.CreateProgram();

        var shader = new ShaderProgram(gl, handle);

        gl.AttachShader(handle, vertexHandle);
        gl.AttachShader(handle, fragmentHandle);
        gl.LinkProgram(handle);

        gl.GetProgram(handle, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
        {
            return Result.Failure<ShaderProgram, CreateShaderProgramError>(new CreateShaderProgramError(new ShaderProgramLinkFailed(gl.GetProgramInfoLog(handle))));
        }

        gl.DetachShader(handle, vertexHandle);
        gl.DetachShader(handle, fragmentHandle);
        gl.DeleteShader(vertexHandle);
        gl.DeleteShader(fragmentHandle);

        return Result.Success<ShaderProgram, CreateShaderProgramError>(shader);

    } 
}

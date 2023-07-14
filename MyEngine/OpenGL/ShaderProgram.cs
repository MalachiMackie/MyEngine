using Silk.NET.OpenGL;
using System.Numerics;

namespace MyEngine.OpenGL
{
    internal class ShaderProgram : IDisposable
    {
        private GL _gl;
        private uint _handle;

        public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
        {
            _gl = gl;

            var vertexHandle = LoadShader(ShaderType.VertexShader, vertexSource);
            var fragmentHandle = LoadShader(ShaderType.FragmentShader, fragmentSource);

            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, vertexHandle);
            _gl.AttachShader(_handle, fragmentHandle);
            _gl.LinkProgram(_handle);

            _gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out int lStatus);
            if (lStatus != (int)GLEnum.True)
            {
                throw new Exception($"Shader program failed to link: {_gl.GetProgramInfoLog(_handle)}");
            }

            _gl.DetachShader(_handle, vertexHandle);
            _gl.DetachShader(_handle, fragmentHandle);
            _gl.DeleteShader(vertexHandle);
            _gl.DeleteShader(fragmentHandle);
        }

        private uint LoadShader(ShaderType shaderType, string source)
        {
            var handle = _gl.CreateShader(shaderType);
            _gl.ShaderSource(handle, source);
            _gl.CompileShader(handle);

            _gl.GetShader(handle, ShaderParameterName.CompileStatus, out int status);
            if (status != (int)GLEnum.True)
            {
                throw new Exception($"{shaderType} shader failed to compile: {_gl.GetShaderInfoLog(handle)}");
            }

            return handle;
        }

        public void SetUniform1(string uniformName, int value)
        {
            var location = _gl.GetUniformLocation(_handle, uniformName);
            _gl.Uniform1(location, value);
        }

        public unsafe void SetUniform1(string uniformName, Matrix4x4 matrix)
        {
            var location = _gl.GetUniformLocation( _handle, uniformName);
            _gl.UniformMatrix4(location, 1, false, (float*) &matrix);
        }

        public void UseProgram()
        {
            _gl.UseProgram(_handle);
        }

        public void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }
    }
}

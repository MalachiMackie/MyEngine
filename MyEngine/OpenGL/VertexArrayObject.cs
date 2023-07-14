using Silk.NET.OpenGL;

namespace MyEngine.OpenGL
{
    internal class VertexArrayObject : IDisposable
    {
        private readonly uint _handle;
        private readonly GL _gl;

        public VertexArrayObject(GL gl)
        {
            _gl = gl;

            _handle = _gl.GenVertexArray();
            Bind();
        }

        public unsafe void VertexArrayAttribute(uint location, int count, VertexAttribPointerType type, bool normalized, uint stride, uint offset)
        {
            _gl.EnableVertexAttribArray(location);
            _gl.VertexAttribPointer(location, count, type, normalized, stride, (void*)offset);
        }

        public void Bind()
        {
            _gl.BindVertexArray(_handle);
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
        }
    }
}

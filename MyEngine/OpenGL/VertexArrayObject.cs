using Silk.NET.OpenGL;

namespace MyEngine.OpenGL
{
    internal class VertexArrayObject : IDisposable
    {
        private uint _handle;
        private GL _gl;

        public VertexArrayObject(GL gl)
        {
            _gl = gl;

            _handle = _gl.GenVertexArray();
            Bind();
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

using Silk.NET.OpenGL;

namespace MyEngine.OpenGL
{
    internal class VertexArrayObject<TVertexType, TIndexType> : IDisposable
        where TVertexType : unmanaged
        where TIndexType : unmanaged
    {
        private readonly uint _handle;
        private readonly GL _gl;

        public VertexArrayObject(GL gl, BufferObject<TVertexType> vertexBuffer, BufferObject<TIndexType> indexBuffer)
        {
            _gl = gl;

            _handle = _gl.GenVertexArray();
            Bind();

            vertexBuffer.Bind();
            indexBuffer.Bind();
        }

        public unsafe void VertexArrayAttribute(uint location, int count, VertexAttribPointerType type, bool normalized, uint vertexSize, uint offsetSize)
        {
            _gl.EnableVertexAttribArray(location);
            _gl.VertexAttribPointer(location, count, type, normalized, (uint)(vertexSize * sizeof(TVertexType)), (void*)(offsetSize * sizeof(TVertexType)));
        }

        public void Bind()
        {
            _gl.BindVertexArray(_handle);
        }

        public void Unbind()
        {
            _gl.BindVertexArray(0);
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
        }
    }
}

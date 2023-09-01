using Silk.NET.OpenGL;

namespace MyEngine.Rendering.OpenGL;

internal class VertexArrayObject : IDisposable
{
    private readonly uint _handle;
    private readonly uint _vertexDataTypeSize;
    private readonly GL _gl;

    private VertexArrayObject(GL gl, uint handle, uint vertexDataTypeSize)
    {
        _gl = gl;
        _vertexDataTypeSize = vertexDataTypeSize;
        _handle = handle;
    }

    public unsafe void VertexArrayAttribute(uint location, int count, VertexAttribPointerType type, bool normalized, uint vertexSize, uint offsetSize)
    {
        _gl.EnableVertexAttribArray(location);
        _gl.VertexAttribPointer(location, count, type, normalized, vertexSize * _vertexDataTypeSize, (void*)(offsetSize * _vertexDataTypeSize));
    }

    public void AttachBuffer<TElementType>(BufferObject<TElementType> elementBuffer)
        where TElementType : unmanaged
    {
        elementBuffer.Bind();
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

    public unsafe static VertexArrayObject CreateAndBind<TVertexType>(GL gl, BufferObject<TVertexType> vertexBuffer)
        where TVertexType : unmanaged
    {
        var handle = gl.GenVertexArray();
        var vertexArrayObject = new VertexArrayObject(gl, handle, (uint)sizeof(TVertexType));

        vertexArrayObject.Bind();
        vertexBuffer.Bind();

        return vertexArrayObject;
    }
}

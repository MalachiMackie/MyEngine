using Silk.NET.OpenGL;

namespace MyEngine.Rendering.OpenGL;

internal class VertexArrayObject : IDisposable
{
    private readonly uint _handle;
    private readonly GL _gl;

    private VertexArrayObject(GL gl, uint handle)
    {
        _gl = gl;
        _handle = handle;
    }

    public unsafe void VertexArrayAttribute(
        uint location,
        int count,
        VertexAttribPointerType type,
        uint pointerTypeSize,
        bool normalized,
        uint vertexSize,
        uint offsetSize)
    {
        _gl.EnableVertexAttribArray(location);
        _gl.VertexAttribPointer(location, count, type, normalized, vertexSize * pointerTypeSize, (void*)(offsetSize * pointerTypeSize));
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

    public unsafe static VertexArrayObject CreateAndBind(GL gl)
    {
        var handle = gl.GenVertexArray();
        var vertexArrayObject = new VertexArrayObject(gl, handle);

        vertexArrayObject.Bind();

        return vertexArrayObject;
    }
}

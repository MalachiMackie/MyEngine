﻿using Silk.NET.OpenGL;

namespace MyEngine.Silk.NET.Rendering;

internal class BufferObject : IDisposable
{
    protected readonly GL GL;
    protected readonly uint Handle;
    protected readonly BufferTargetARB Target;
    protected readonly BufferUsageARB Usage;

    protected BufferObject(GL gL, uint handle, BufferTargetARB target, BufferUsageARB usage)
    {
        GL = gL;
        Handle = handle;
        Target = target;
        Usage = usage;
    }

    public void Bind()
    {
        GL.BindBuffer(Target, Handle);
    }

    public void Unbind()
    {
        GL.BindBuffer(Target, 0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(Handle);
    }
}

internal class BufferObject<TData> : BufferObject
    where TData : unmanaged
{
    private BufferObject(GL gl, uint handle, BufferTargetARB target, BufferUsageARB usage) : base(gl, handle, target, usage)
    {
    }

    public void SetData(ReadOnlySpan<TData> data)
    {
        GL.BufferData(Target, data, Usage);
    }

    public static BufferObject<TData> CreateAndBind(GL gl, BufferTargetARB target, BufferUsageARB usage)
    {
        var handle = gl.GenBuffer();
        var bufferObject = new BufferObject<TData>(gl, handle, target, usage);

        bufferObject.Bind();

        return bufferObject;
    }
}

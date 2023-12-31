﻿using Silk.NET.OpenGL;

namespace MyEngine.Rendering.OpenGL;

internal class TextureObject : IDisposable
{
    private readonly uint _handle;
    private readonly GL _gl;
    private readonly TextureTarget _target;

    public TextureObject(
        GL gl,
        byte[] bytes,
        uint width,
        uint height,
        TextureTarget target)
    {
        _gl = gl;

        _handle = _gl.GenTexture();
        _target = target;

        _gl.BindTexture(_target, _handle);

        _gl.TexImage2D<byte>(
            _target,
            level: 0,
            InternalFormat.Rgba,
            width,
            height,
            border: 0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            bytes.AsSpan());

        SetParameters();
    }

    private void SetParameters()
    {
        _gl.TextureParameter(_handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TextureParameter(_handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        _gl.TextureParameter(_handle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TextureParameter(_handle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        _gl.GenerateMipmap(_target);
    }

    public void Bind(uint slot)
    {
        if (slot > 31)
        {
            throw new InvalidOperationException("Texture slot must be less than 32");
        }

        _gl.BindTextureUnit(slot, _handle);
    }

    public void Unbind()
    {
        _gl.BindTexture(_target, 0);
    }

    public void Dispose()
    {
        _gl.DeleteTexture(_handle);
    }
}

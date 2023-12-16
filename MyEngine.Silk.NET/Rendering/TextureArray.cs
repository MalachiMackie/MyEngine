using Silk.NET.OpenGL;

namespace MyEngine.Rendering.OpenGL;

internal class TextureArray
{
    public const uint MaxTextures = 32;

    private readonly uint _handle;
    private readonly GL _gl;
    private readonly TextureTarget _target;
    private readonly byte[] _clearData;
    public uint Width { get; }
    public uint Height { get; }

    private uint _textureCount;

    public TextureArray(
        GL gl,
        uint maxWidth,
        uint maxHeight,
        TextureTarget target)
    {
        _gl = gl;
        _target = target;
        _handle = _gl.GenTexture();
        Width = maxWidth;
        Height = maxHeight;

        _clearData = new byte[Width * Height * 4];
        Array.Fill<byte>(_clearData, 0);

        _gl.BindTexture(_target, _handle);
        _gl.TexStorage3D(_target, 1, SizedInternalFormat.Rgba8, Width, Height, MaxTextures);

        _gl.TexParameterI(_target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);

        _gl.TexParameterI(_target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        _gl.TexParameterI(_target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TexParameterI(_target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
    }

    public uint AddTexture(Span<byte> data, uint xOffset, uint yOffset, uint width, uint height, bool newTexture)
    {
        var slot = _textureCount;

        if (newTexture)
        {
            _gl.TexSubImage3D<byte>(
                _target,
                level: 0,
                xoffset: 0,
                yoffset: 0,
                zoffset: (int)slot,
                Width,
                Height,
                depth: 1,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                _clearData.AsSpan());

            _textureCount++;
        }

        _gl.TexSubImage3D<byte>(
            _target,
            level: 0,
            (int)xOffset,
            (int)yOffset,
            zoffset: (int)slot,
            width,
            height,
            depth: 1,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            data);

        _gl.GenerateMipmap(_target);

        return slot;
    }

    public void Bind()
    {
        _gl.BindTexture(_target, _handle);
    }
}

using Silk.NET.OpenGL;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.OpenGL
{
    internal class TextureObject : IDisposable
    {
        private readonly uint _handle;
        private readonly GL _gl;
        private readonly TextureTarget _target;

        private unsafe TextureObject(
            GL gl,
            byte[] bytes,
            uint width,
            uint height,
            TextureTarget target,
            TextureUnit unit)
        {
            _gl = gl;

            _handle = _gl.GenTexture();
            _target = target;

            Bind(unit);

            fixed (byte* ptr = bytes)
            {
                _gl.TexImage2D(
                    _target,
                    0,
                    InternalFormat.Rgba,
                    width,
                    height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr);
            }

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

        public void Bind(TextureUnit unit)
        {
            _gl.ActiveTexture(unit);
            _gl.BindTexture(_target, _handle);
        }

        public static TextureObject Create(GL gl, string path, TextureTarget target, TextureUnit unit)
        {
            var bytes = File.ReadAllBytes(path);
            var imageResult = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
            return new TextureObject(gl, imageResult.Data, (uint)imageResult.Width, (uint)imageResult.Height, target, unit);
        }

        public void Dispose()
        {
            _gl.DeleteTexture(_handle);
        }
    }
}

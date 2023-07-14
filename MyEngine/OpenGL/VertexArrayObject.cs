using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

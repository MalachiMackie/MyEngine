﻿using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.OpenGL
{
    internal class BufferObject<TData> : IDisposable
        where TData : unmanaged
    {
        private readonly GL _gl;
        private readonly uint _handle;
        private readonly BufferTargetARB _target;

        public unsafe BufferObject(GL gl, TData[] data, BufferTargetARB target, BufferUsageARB usage)
        {
            _gl = gl;
            _handle = _gl.GenBuffer();
            _target = target;

            Bind();

            fixed (TData* ptr = data)
            {
                _gl.BufferData(_target, (nuint)(data.Length * sizeof(TData)), ptr, usage);
            }
        }

        public void Bind()
        {
            _gl.BindBuffer(_target, _handle);
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
        }
    }
}

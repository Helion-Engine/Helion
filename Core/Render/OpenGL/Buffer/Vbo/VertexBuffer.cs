using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Buffer.Vbo
{
    /// <summary>
    /// A wrapper around a vertex buffer object.
    /// </summary>
    public abstract class VertexBuffer<T> : IDisposable where T : struct
    {
        protected bool NeedsUploading;
        private bool disposed;
        private int vbo;
        private int typeByteSize;
        private DynamicArray<T> data = new DynamicArray<T>();

        public bool Empty => data.Length == 0;
        public bool NotEmpty => !Empty;

        protected VertexBuffer()
        {
            vbo = GL.GenBuffer();
            typeByteSize = Marshal.SizeOf(new T());
        }

        ~VertexBuffer() => Dispose(false);

        protected abstract BufferUsageHint GetHint();

        protected void DeployData()
        {
            if (NeedsUploading)
            {
                GL.BufferData(BufferTarget.ArrayBuffer, typeByteSize * data.Length, data.Data, GetHint());
                NeedsUploading = false;
            }
        }

        public void Add(T element)
        {
            data.Add(element);
            NeedsUploading = true;
        }

        public void Add(params T[] elements)
        {
            foreach (T element in elements)
                data.Add(element);
            if (elements.Length > 0)
                NeedsUploading = true;
        }

        public void Add(IList<T> elements)
        {
            foreach (T element in elements)
                data.Add(element);
            if (elements.Count > 0)
                NeedsUploading = true;
        }

        public virtual void Clear() => data.Clear();

        protected void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            DeployData();
        } 

        protected virtual void Unbind() => GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        public void BindAnd(Action action)
        {
            Bind();
            action.Invoke();
            Unbind();
        }

        public void BindAndDraw()
        {
            BindAnd(() => { GL.DrawArrays(PrimitiveType.Triangles, 0, data.Length); });
        }

        public void BindAndDrawIfNotEmpty()
        {
            if (NotEmpty)
                BindAndDraw();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GL.DeleteBuffer(vbo);

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

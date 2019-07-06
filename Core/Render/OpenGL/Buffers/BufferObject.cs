using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Buffers
{
    public abstract class BufferObject<T> : IDisposable where T : struct
    {
        protected bool uploaded;
        protected int bufferHandle;
        protected DynamicArray<T> data = new DynamicArray<T>();
        private readonly BufferTarget target;
        private readonly BufferUsageHint hint;
        private readonly int typeByteSize = Marshal.SizeOf<T>();
        private bool disposed;
        
        public int Count => data.Length;

        protected BufferObject(BufferTarget bufferTarget, BufferUsageHint usageHint, int glObjectId)
        {
            // TODO: Write something that asserts every field offset is packed.
            Invariant(typeof(T).StructLayoutAttribute.Pack == 1, $"Type {typeof(T)} does not pack its data tightly");

            target = bufferTarget;
            hint = usageHint;
            bufferHandle = glObjectId;
        }
        
        ~BufferObject()
        {
            ReleaseUnmanagedResources();
        }
        
        protected virtual void ReleaseUnmanagedResources()
        {
            Precondition(!disposed, "Attempting to dispose a GL buffer more than once");
            
            GL.DeleteBuffer(bufferHandle);
            disposed = true;
        }
        
        public void Add(T element)
        {
            data.Add(element);
            MarkAsNeedingUpload();
        }

        public void Add(params T[] elements)
        {
            if (elements.Length > 0)
            {
                data.Add(elements);
                MarkAsNeedingUpload();
            }
        }

        public void Add(DynamicArray<T> elements)
        {
            Add(elements.Data);
        }
        
        public void Upload()
        {
            GL.BufferData(target, typeByteSize * data.Length, data.Data, hint);
            uploaded = true;
        }
        
        public void Clear()
        {
            data.Clear();
        }
        
        public void Bind()
        {
            GL.BindBuffer(target, bufferHandle);
        }

        public void Unbind()
        {
            GL.BindBuffer(target, 0);   
        }

        public void BindAnd(Action action)
        {
            Bind();
            action.Invoke();
            Unbind();
        }
        
        [Conditional("DEBUG")]
        protected void MarkAsNeedingUpload()
        {
            uploaded = false;
        }
        
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            
            // Since VBOs can end up holding a lot of data, if we dispose of it
            // but take a while to lose the reference, we still want to leave
            // the option for the GC to retrieve memory.
#nullable disable
            data = null;
#nullable enable

            GC.SuppressFinalize(this);
        }
    }
}
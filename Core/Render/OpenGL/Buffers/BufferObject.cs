using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Helion.Render.OpenGL.Util;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Buffers
{
    public abstract class BufferObject<T> : IDisposable where T : struct
    {
        protected bool Uploaded;
        protected int BufferHandle;
        protected DynamicArray<T> Data = new DynamicArray<T>();
        private readonly BufferTarget target;
        private readonly BufferUsageHint hint;
        private readonly int typeByteSize = Marshal.SizeOf<T>();

        public int Count => Data.Length;
        public bool NeedsUpload => !Uploaded;

        protected BufferObject(GLCapabilities capabilities, BufferTarget bufferTarget, 
            BufferUsageHint usageHint, int glObjectId)
        {
            // TODO: Write something that asserts every field offset is packed.
            Invariant(typeof(T).StructLayoutAttribute.Pack == 1, $"Type {typeof(T)} does not pack its data tightly");

            target = bufferTarget;
            hint = usageHint;
            BufferHandle = glObjectId;
        }
        
        ~BufferObject()
        {
            ReleaseUnmanagedResources();
        }

        public void Add(T element)
        {
            Data.Add(element);
            Uploaded = false;
        }

        public void Add(params T[] elements)
        {
            if (elements.Length > 0)
            {
                Data.Add(elements);
                Uploaded = false;
            }
        }

        public void Add(DynamicArray<T> elements)
        {
            Add(elements.Data);
        }

        public void Upload()
        {
            GL.BufferData(target, typeByteSize * Data.Length, Data.Data, hint);
            Uploaded = true;
        }

        public void Clear()
        {
            Data.Clear();
        }

        public void BindAnd(Action action)
        {
            Bind();
            action.Invoke();
            Unbind();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            
            // Since VBOs can end up holding a lot of data, if we dispose of it
            // but take a while to lose the reference, we still want to leave
            // the option for the GC to retrieve memory.
#nullable disable
            Data = null;
#nullable enable

            GC.SuppressFinalize(this);
        }

        protected virtual void ReleaseUnmanagedResources()
        {
            GL.DeleteBuffer(BufferHandle);
        }
        
        protected void Bind()
        {
            GL.BindBuffer(target, BufferHandle);
        }

        protected void Unbind()
        {
            GL.BindBuffer(target, 0);   
        }
    }
}
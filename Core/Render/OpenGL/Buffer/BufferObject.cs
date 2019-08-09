using System;
using System.Runtime.InteropServices;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Util.Container;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Buffer
{
    public abstract class BufferObject<T> : IDisposable where T : struct
    {
        public static readonly int BytesPerElement = Marshal.SizeOf<T>();
        
        protected readonly int BufferId;
        protected readonly GLFunctions gl;
        protected DynamicArray<T> Data = new DynamicArray<T>();
        protected bool Uploaded;

        public int Count => Data.Length;
        public bool NeedsUpload => !Uploaded;

        protected BufferObject(GLCapabilities capabilities, GLFunctions functions, string objectLabel = "")
        {
            gl = functions;
            BufferId = gl.GenBuffer();

            BindAnd(() => { SetObjectLabel(capabilities, objectLabel); });
        }

        ~BufferObject()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public void Add(T element)
        {
            Data.Add(element);
            Uploaded = false;
        }

        public void Add(params T[] elements)
        {
            if (elements.Length <= 0)
                return;
            
            Data.Add(elements);
            Uploaded = false;
        }

        public void Upload()
        {
            if (Uploaded)
                return;

            PerformUpload();
            Uploaded = true;
        }

        public void Clear()
        {
            Data.Clear();
            Uploaded = false;
        }

        public void Bind()
        {
            gl.BindBuffer(GetBufferType(), BufferId);
        }

        public void Unbind()
        {
            gl.BindBuffer(GetBufferType(), 0);   
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
            GC.SuppressFinalize(this);
        }
        
        protected abstract BufferType GetBufferType();
        protected abstract void PerformUpload();
        protected abstract void SetObjectLabel(GLCapabilities capabilities, string objectLabel);

        private void ReleaseUnmanagedResources()
        {
            // Since VBOs can end up holding a lot of data, if we dispose of it
            // but take a while to lose the reference, we still want to leave
            // the option for the GC to retrieve memory.
#nullable disable
            Data = null;
#nullable enable
            
            gl.DeleteBuffer(BufferId);
        }
    }
}
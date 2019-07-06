using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Buffers
{
    public class StaticVertexBufferObject<T> : VertexBufferObject<T> where T : struct
    {
        public StaticVertexBufferObject(VertexArrayObject vao) : base(BufferUsageHint.StaticDraw, vao)
        {
        }
    }
    
    public class DynamicVertexBufferObject<T> : VertexBufferObject<T> where T : struct
    {
        public DynamicVertexBufferObject(VertexArrayObject vao) : base(BufferUsageHint.DynamicDraw, vao)
        {
        }
    }

    public class StreamVertexBufferObject<T> : VertexBufferObject<T> where T : struct
    {
        public StreamVertexBufferObject(VertexArrayObject vao) : base(BufferUsageHint.StreamDraw, vao)
        {
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack=0)]
    public class VertexBufferObject<T> : IDisposable where T : struct
    {
        private readonly int vbo;
        private readonly BufferUsageHint hint;
        private readonly int typeByteSize = Marshal.SizeOf(new T());
        private DynamicArray<T> data = new DynamicArray<T>();
        private bool uploaded = true;
        private bool disposed;
        
        public int Count => data.Length;
        
        protected VertexBufferObject(BufferUsageHint usageHint, VertexArrayObject vao)
        {
            // TODO: Write something that asserts every field offset is packed.
            Invariant(typeof(T).StructLayoutAttribute.Pack == 1, $"Type {typeof(T)} does not pack its data tightly");
            
            vbo = GL.GenBuffer();
            hint = usageHint;

            BindAttributes(vao);
        }
        
        public void BindAttributes(VertexArrayObject vao)
        {
            vao.BindAnd(() =>
            {
                BindAnd(() =>
                {
                    int stride = vao.Attributes.Select(attr => attr.ByteLength()).Sum();
                    int offset = 0;
                    foreach (VertexArrayAttribute attr in vao.Attributes)
                    {
                        attr.Enable(stride, offset);
                        offset += attr.ByteLength();
                    }
                    
                    Postcondition(stride == Marshal.SizeOf(typeof(T)), $"VAO attributes do not match struct '{typeof(T).Name}' size, attributes should map onto struct offsets");
                });
            });
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

        public void Clear()
        {
            data.Clear();
        }
        
        public void Upload()
        {
            GL.BufferData(BufferTarget.ArrayBuffer, typeByteSize * data.Length, data.Data, hint);
            uploaded = true;
        }
        
        public void DrawArrays()
        {
            Precondition(uploaded, "Forgot to upload VBO");
            GL.DrawArrays(PrimitiveType.Triangles, 0, Count);
        }
        
        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        }

        public void Unbind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);   
        }

        public void BindAnd(Action action)
        {
            Bind();
            action.Invoke();
            Unbind();
        }

        [Conditional("DEBUG")]
        private void MarkAsNeedingUpload()
        {
            uploaded = false;
        }

        private void ReleaseUnmanagedResources()
        {
            Precondition(!disposed, "Attempting to dispose a VBO more than once");
            
            GL.DeleteBuffer(vbo);
            disposed = true;
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

        ~VertexBufferObject()
        {
            ReleaseUnmanagedResources();
        }
    }
}
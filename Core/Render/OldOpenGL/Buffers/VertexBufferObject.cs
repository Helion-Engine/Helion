using System.Linq;
using System.Runtime.InteropServices;
using Helion.Render.OpenGL.Old.Util;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Old.Buffers
{
    public class VertexBufferObject<T> : BufferObject<T> where T : struct
    {
        protected VertexBufferObject(GLCapabilities capabilities, BufferUsageHint hint, VertexArrayObject vao,
                string objectLabel = "") : 
            base(capabilities, BufferTarget.ArrayBuffer, hint, GL.GenBuffer())
        {
            BindAttributes(vao);

            // We need to at least bind it first to allocate it, otherwise it's
            // undefined behavior to apply a label.
            BindAnd(() => { GLHelper.SetBufferLabel(capabilities, BufferHandle, objectLabel); });
        }

        private void BindAttributes(VertexArrayObject vao)
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

                    Postcondition(stride == Marshal.SizeOf<T>(), $"VAO attributes do not match struct '{typeof(T).Name}' size, attributes should map onto struct offsets");
                });
            });
        }

        public void DrawArrays()
        {
            if (Count == 0)
                return;
            
            Precondition(Uploaded, "Forgot to upload VBO data");
            GL.DrawArrays(PrimitiveType.Triangles, 0, Count);
        }
    }
    
    public class StaticVertexBufferObject<T> : VertexBufferObject<T> where T : struct
    {
        public StaticVertexBufferObject(GLCapabilities capabilities, VertexArrayObject vao, string objectLabel = "") : 
            base(capabilities, BufferUsageHint.StaticDraw, vao, objectLabel)
        {
        }
    }
    
    public class DynamicVertexBufferObject<T> : VertexBufferObject<T> where T : struct
    {
        public DynamicVertexBufferObject(GLCapabilities capabilities, VertexArrayObject vao, string objectLabel = "") : 
            base(capabilities, BufferUsageHint.DynamicDraw, vao, objectLabel)
        {
        }
    }

    public class StreamVertexBufferObject<T> : VertexBufferObject<T> where T : struct
    {
        public StreamVertexBufferObject(GLCapabilities capabilities, VertexArrayObject vao, string objectLabel = "") : 
            base(capabilities, BufferUsageHint.StreamDraw, vao, objectLabel)
        {
        }
    }
}
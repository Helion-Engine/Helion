using System.Linq;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Buffers
{
    public class VertexBufferObject<T> : BufferObject<T> where T : struct
    {
        protected VertexBufferObject(BufferUsageHint hint, VertexArrayObject vao) : base(BufferTarget.ArrayBuffer, hint,
            GL.GenBuffer())
        {
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

                    Postcondition(stride == Marshal.SizeOf<T>(),
                        $"VAO attributes do not match struct '{typeof(T).Name}' size, attributes should map onto struct offsets");
                });
            });
        }

        public void DrawArrays()
        {
            Precondition(uploaded, "Forgot to upload VBO data");
            GL.DrawArrays(PrimitiveType.Triangles, 0, Count);
        }
    }
    
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
}
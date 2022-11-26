using System.Linq;
using System.Runtime.InteropServices;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Buffer.Array.Vertex;

public abstract class VertexBufferObject<T> : ArrayBufferObject<T> where T : struct
{
    public VertexBufferObject(VertexArrayObject vao, string objectLabel) : base(objectLabel)
    {
        BindAttributes(vao);
    }

    public void DrawArrays()
    {
        if (Count == 0)
            return;

        Precondition(Uploaded, "Forgot to upload VBO data");
        GL.DrawArrays(PrimitiveType.Triangles, 0, Count);
    }

    private void BindAttributes(VertexArrayObject vao)
    {
        vao.Bind();
        Bind();

        int offset = 0;
        for (int i = 0; i < vao.Attributes.AttributesArray.Length; i++)
        {
            vao.Attributes.AttributesArray[i].Enable(vao.Attributes.Stride, offset);
            offset += vao.Attributes.AttributesArray[i].ByteLength();
        }

        Unbind();
        vao.Unbind();

        Postcondition(vao.Attributes.Stride == Marshal.SizeOf<T>(), "VAO attributes do not match target struct size, attributes should map onto struct offsets");
    }
}

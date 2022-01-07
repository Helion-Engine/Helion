using System.Linq;
using System.Runtime.InteropServices;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Render.Legacy.Vertex;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Buffer.Array.Vertex;

public abstract class VertexBufferObject<T> : ArrayBufferObject<T> where T : struct
{
    public VertexBufferObject(GLCapabilities capabilities, IGLFunctions functions, VertexArrayObject vao, string objectLabel = "") :
        base(capabilities, functions, objectLabel)
    {
        BindAttributes(vao);
    }

    public void DrawArrays()
    {
        if (Count == 0)
            return;

        Precondition(Uploaded, "Forgot to upload VBO data");
        gl.DrawArrays(PrimitiveDrawType.Triangles, 0, Count);
    }

    private void BindAttributes(VertexArrayObject vao)
    {
        vao.Bind();
        Bind();
        int offset = 0;
        for (int i = 0; i < vao.Attributes.AttributesArray.Length; i++)
        {
            vao.Attributes.AttributesArray[i].Enable(gl, vao.Attributes.Stride, offset);
            offset += vao.Attributes.AttributesArray[i].ByteLength();
        }

        Postcondition(vao.Attributes.Stride == Marshal.SizeOf<T>(), "VAO attributes do not match target struct size, attributes should map onto struct offsets");
        Unbind();
        vao.Unbind();
    }
}

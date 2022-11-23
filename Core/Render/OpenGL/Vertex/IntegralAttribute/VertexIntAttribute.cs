using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Vertex.IntegralAttribute;

public abstract class VertexIntAttribute : VertexArrayAttribute
{
    public VertexIntAttribute(string name, int index, int size) : base(name, index, size)
    {
    }

    public override void Enable(IGLFunctions gl, int stride, int offset)
    {
        Precondition(stride >= ByteLength(), "Stride is smaller than the length of the VAO element");
        Precondition(offset >= 0 && offset < stride, "Offset relative to stride is wrong");

        gl.VertexAttribIPointer(Index, Size, GetAttributeType(), stride, offset);
        gl.EnableVertexAttribArray(Index);
    }

    protected abstract VertexAttributeIntegralPointerType GetAttributeType();
}

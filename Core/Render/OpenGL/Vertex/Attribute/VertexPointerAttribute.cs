using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Vertex.Attribute;

public abstract class VertexPointerAttribute : VertexArrayAttribute
{
    public readonly bool Normalized;

    public VertexPointerAttribute(string name, int index, int size, bool normalized) :
        base(name, index, size)
    {
        Normalized = normalized;
    }

    public override void Enable(IGLFunctions gl, int stride, int offset)
    {
        Precondition(stride >= ByteLength(), "Stride is smaller than the length of the VAO element");
        Precondition(offset >= 0 && offset < stride, "Offset relative to stride is wrong");

        gl.VertexAttribPointer(Index, Size, GetAttributePointerType(), Normalized, stride, offset);
        gl.EnableVertexAttribArray(Index);
    }

    protected abstract VertexAttributePointerType GetAttributePointerType();
}

using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Vertex.Attributes;

public abstract class VertexPointerAttribute : VertexArrayAttribute
{
    public readonly bool Normalized;

    protected abstract VertexAttribPointerType AttributeType { get; }

    public VertexPointerAttribute(string name, int index, int size, bool normalized) :
        base(name, index, size)
    {
        Normalized = normalized;
    }

    public override void Enable(int stride, int offset)
    {
        Precondition(stride >= ByteLength, "Stride is smaller than the length of the VAO element");
        Precondition(offset >= 0 && offset < stride, "Offset relative to stride is wrong");

        GL.VertexAttribPointer(Index, Size, AttributeType, Normalized, stride, offset);
        GL.EnableVertexAttribArray(Index);
    }
}

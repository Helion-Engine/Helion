using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Vertex.Attributes;

public abstract class VertexIntAttribute : VertexArrayAttribute
{
    protected abstract VertexAttribIntegerType AttributeType { get; }

    public VertexIntAttribute(string name, int index, int size) : base(name, index, size)
    {
    }

    public override void Enable(int stride, int offset)
    {
        Precondition(stride >= ByteLength, "Stride is smaller than the length of the VAO element");
        Precondition(offset >= 0 && offset < stride, "Offset relative to stride is wrong");

        GL.VertexAttribIPointer(Index, Size, AttributeType, stride, new(offset));
        GL.EnableVertexAttribArray(Index);
    }
}

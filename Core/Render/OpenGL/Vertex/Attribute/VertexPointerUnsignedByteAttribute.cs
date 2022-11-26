using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Vertex.Attribute;

public class VertexPointerUnsignedByteAttribute : VertexPointerAttribute
{
    public override int ByteLength => 1 * Size;
    protected override VertexAttribPointerType AttributeType => VertexAttribPointerType.UnsignedByte;

    public VertexPointerUnsignedByteAttribute(string name, int index, int size, bool normalized = false) :
        base(name, index, size, normalized)
    {
    }
}

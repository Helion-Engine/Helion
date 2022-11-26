using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Vertex.Attribute;

public class VertexPointerUnsignedByteAttribute : VertexPointerAttribute
{
    public VertexPointerUnsignedByteAttribute(string name, int index, int size, bool normalized = false) :
        base(name, index, size, normalized)
    {
    }

    public override int ByteLength() => 1 * Size;

    protected override VertexAttribPointerType GetAttributePointerType() => VertexAttribPointerType.UnsignedByte;
}

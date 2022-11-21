using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Vertex.Attribute;

namespace Helion.Render.OpenGL.Vertex.Attribute;

public class VertexPointerUnsignedByteAttribute : VertexPointerAttribute
{
    public VertexPointerUnsignedByteAttribute(string name, int index, int size, bool normalized = false) :
        base(name, index, size, normalized)
    {
    }

    public override int ByteLength() => 1 * Size;

    protected override VertexAttributePointerType GetAttributePointerType()
    {
        return VertexAttributePointerType.UnsignedByte;
    }
}

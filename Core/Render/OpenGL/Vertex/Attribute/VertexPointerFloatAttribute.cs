using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Vertex.Attribute;

public class VertexPointerFloatAttribute : VertexPointerAttribute
{
    public override int ByteLength => sizeof(float) * Size;
    protected override VertexAttribPointerType AttributeType => VertexAttribPointerType.Float;

    public VertexPointerFloatAttribute(string name, int index, int size, bool normalized = false) :
        base(name, index, size, normalized)
    {
    }
}

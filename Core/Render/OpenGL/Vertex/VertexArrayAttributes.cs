using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Vertex;

public class VertexArrayAttributes
{
    public readonly VertexArrayAttribute[] AttributesArray;
    public readonly int Stride;

    public VertexArrayAttributes(params VertexArrayAttribute[] attributes)
    {
        Precondition(attributes.Length > 0, "Cannot have a VAO with no attributes");

        AttributesArray = attributes;
        for (int i = 0; i < attributes.Length; i++)
            Stride += attributes[i].ByteLength;
    }
}

using System.Collections.Generic;
using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Vertex;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Vertex;

public class VertexArrayAttributes
{
    public VertexArrayAttribute[] AttributesArray;
    public int Stride;

    public VertexArrayAttributes(params VertexArrayAttribute[] vaoAttributes)
    {
        Precondition(vaoAttributes.Length > 0, "Cannot have a VAO with no attributes");

        AttributesArray = vaoAttributes;
        Stride = 0;
        for (int i = 0; i < vaoAttributes.Length; i++)
            Stride += vaoAttributes[i].ByteLength();
    }
}

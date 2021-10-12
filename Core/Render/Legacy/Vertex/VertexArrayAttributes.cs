using System.Collections;
using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Vertex;

public class VertexArrayAttributes : IEnumerable<VertexArrayAttribute>
{
    private readonly List<VertexArrayAttribute> m_attributes = new List<VertexArrayAttribute>();

    public int Count => m_attributes.Count;

    public VertexArrayAttributes(params VertexArrayAttribute[] vaoAttributes)
    {
        Precondition(vaoAttributes.Length > 0, "Cannot have a VAO with no attributes");

        m_attributes.AddRange(vaoAttributes);
    }

    public IEnumerator<VertexArrayAttribute> GetEnumerator() => m_attributes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


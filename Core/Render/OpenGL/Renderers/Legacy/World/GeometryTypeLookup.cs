using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using System;
using System.Collections;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class GeometryTypeLookup<T>
{
    private readonly T[] m_items;

    public GeometryTypeLookup(Func<T> allocator)
    {
        m_items = new T[(int)GeometryType.Count];
        for (int i = 0; i < m_items.Length; i++)
            m_items[i] = allocator();
    }

    public T Get(GeometryType type) => m_items[(int)type];

    public T[] GetItems() => m_items;
}

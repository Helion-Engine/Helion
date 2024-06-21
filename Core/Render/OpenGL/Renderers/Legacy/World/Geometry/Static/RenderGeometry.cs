using Helion.World.Geometry;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class RenderGeometry
{
    private readonly GeometryTypeLookup<List<GeometryData>> m_lookup = new(() => new List<GeometryData>());

    public void AddGeometry(GeometryType type, GeometryData data)
    {
        m_lookup.Get(type).Add(data);
    }

    public List<GeometryData> GetGeometry(GeometryType type)
    {
        return m_lookup.Get(type);
    }

    public List<GeometryData>[] GetAllGeometry()
    {
        return m_lookup.GetItems();
    }

    public void ClearVbo()
    {
        var items = m_lookup.GetItems();
        for (int i = 0; i < items.Length; i++)
        {
            var list = items[i];
            for (int j = 0; j < list.Count; j++)
                list[j].Vbo.Clear();
        }
    }

    public void DisposeAndClear()
    {
        var items = m_lookup.GetItems();
        for (int i = 0; i < items.Length; i++)
        {
            var list = items[i];
            for (int j = 0; j < list.Count; j++)
            {
                var data = list[j];
                data.Dispose();
            }
            list.Clear();
        }
    }
}

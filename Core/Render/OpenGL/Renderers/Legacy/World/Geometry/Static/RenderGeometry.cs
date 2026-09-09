using Helion.Util.Container;
using System;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class RenderGeometry
{
    private readonly GeometryTypeLookup<List<GeometryData>> m_lookup = new(() => new List<GeometryData>());
    private readonly Comparison<GeometryData> m_sortByTextureArray;

    public RenderGeometry()
    {
        m_sortByTextureArray = SortByTextureArray;
    }

    public void AddGeometry(GeometryType type, GeometryData data)
    {
        var geometry = m_lookup.Get(type);
        geometry.Add(data);
        geometry.Sort(m_sortByTextureArray);
    }

    private static int SortByTextureArray(GeometryData x, GeometryData y)
    {
        if (x.Texture.IsArray && !y.Texture.IsArray)
            return 0;
        if (!x.Texture.IsArray && y.Texture.IsArray)
            return 1;

        return x.Texture.Dimension.Height.CompareTo(y.Texture.Dimension.Height);
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
            {
                var item = list[j];
                item.Pipeline.Vbo.Data.Data.ZeroArray();
                item.Pipeline.Vbo.Clear();
            }
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

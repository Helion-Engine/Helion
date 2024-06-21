using Helion.World.Geometry;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class RenderGeometry
{
    private readonly List<GeometryData> m_wallGeometry = new();
    private readonly List<GeometryData> m_flatGeometry = new();

    public void AddGeometry(GeometryType type, GeometryData data)
    {
        GetGeometry(type).Add(data);
    }

    public void ClearVbo()
    {
        for (int i = 0; i < m_wallGeometry.Count; i++)
            m_wallGeometry[i].Vbo.Clear();
        for (int i = 0; i < m_flatGeometry.Count; i++)
            m_flatGeometry[i].Vbo.Clear();
    }

    public void DisposeAndClear()
    {
        for (int i = 0; i < m_wallGeometry.Count; i++)
            m_wallGeometry[i].Dispose();
        for (int i = 0; i < m_flatGeometry.Count; i++)
            m_flatGeometry[i].Dispose();

        m_wallGeometry.Clear();
        m_flatGeometry.Clear();
    }

    public List<GeometryData> GetGeometry(GeometryType type)
    {
        if (type == GeometryType.Flat)
            return m_flatGeometry;
        return m_wallGeometry;
    }
}

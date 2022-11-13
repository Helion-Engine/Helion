using Helion.World.Static;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Geometry.Static;

public class FreeGeometryManager
{
    private readonly List<FreeGeometryData> m_data = new();

    public void Add(int textureHandle, StaticGeometryData geometryData)
    {
        m_data.Add(new FreeGeometryData(textureHandle, geometryData));
    }

    public bool GetAndRemove(int textureHandle, int vertexLength, [NotNullWhen(true)] out StaticGeometryData? data)
    {
        for (int i = 0; i < m_data.Count; i++)
        {
            if (m_data[i].TextureHandle == textureHandle && m_data[i].GeometryData.GeometryDataLength >= vertexLength)
            {
                data = m_data[i].GeometryData;
                m_data.RemoveAt(i);
                return true;
            }    
        }

        data = null;
        return false;
    }

    public void Clear()
    {
        m_data.Clear();
    }
}

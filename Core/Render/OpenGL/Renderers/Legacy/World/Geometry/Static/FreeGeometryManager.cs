using Helion.World.Static;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class FreeGeometryManager
{
    private readonly List<FreeGeometryData> m_data = new();

    public void Add(int textureHandle, StaticGeometryData geometryData)
    {
        m_data.Add(new FreeGeometryData(textureHandle, geometryData));
    }

    public bool GetAndRemove(int textureHandle, int vertexLength, [NotNullWhen(true)] out StaticGeometryData? data)
    {
        int minLength = int.MaxValue;
        int minIndex = -1;
        for (int i = 0; i < m_data.Count; i++)
        {
            int geometryLength = m_data[i].GeometryData.GeometryDataLength;
            if (m_data[i].TextureHandle == textureHandle && geometryLength >= vertexLength && geometryLength < minLength)
            {
                minLength = geometryLength;
                minIndex = i;
            }    
        }

        if (minIndex != -1)
        {
            data = new StaticGeometryData(m_data[minIndex].GeometryData.GeometryData, m_data[minIndex].GeometryData.GeometryDataStartIndex, vertexLength);
            m_data.RemoveAt(minIndex);
            return true;
        }

        data = null;
        return false;
    }

    public void Clear()
    {
        m_data.Clear();
    }
}

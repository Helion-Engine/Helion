using Helion.World.Static;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class FreeGeometryManager
{
    private readonly Dictionary<int, FreeGeometryList> m_data = new(128);

    public void Add(int textureHandle, StaticGeometryData geometryData)
    {
        if (!m_data.TryGetValue(textureHandle, out var list))
        {
            list = new();
            m_data[textureHandle] = list;
        }

        if (list.LastReleasedIndex != -1)
        {
            list.Geometry.Data[list.LastReleasedIndex] = new FreeGeometryData(textureHandle, geometryData);
            list.LastReleasedIndex = -1;
            return;
        }

        list.Geometry.Add(new FreeGeometryData(textureHandle, geometryData));
    }

    public bool GetAndRemove(int textureHandle, int vertexLength, [NotNullWhen(true)] out StaticGeometryData? data)
    {
        int minLength = int.MaxValue;
        int minIndex = -1;

        if (!m_data.TryGetValue(textureHandle, out var list))
        {
            data = null;
            return false;
        }
        
        for (int i = 0; i < list.Geometry.Length; i++)
        {
            ref var itemData = ref list.Geometry.Data[i];
            if (itemData.Released)
            {
                if (list.LastReleasedIndex == -1)
                    list.LastReleasedIndex = i;
                continue;
            }

            if (itemData.Geometry.Length >= vertexLength && itemData.Geometry.Length < minLength)
            {
                minLength = itemData.Geometry.Length;
                minIndex = i;
            }
        }

        if (minIndex != -1)
        {
            if (list.LastReleasedIndex == -1)
                list.LastReleasedIndex = minIndex;
            ref var itemData = ref list.Geometry.Data[minIndex];
            data = new StaticGeometryData(itemData.Geometry.GeometryData, itemData.Geometry.Index, vertexLength);
            itemData.Released = true;
            itemData.Geometry.Length = 0;
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

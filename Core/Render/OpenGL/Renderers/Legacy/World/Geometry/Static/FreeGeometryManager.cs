using Helion.Util.Assertion;
using Helion.World.Static;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class FreeGeometryManager
{
    private readonly Dictionary<int, FreeGeometryList> m_data = new(128);

    public void Add(in StaticGeometryData geometryData)
    {
        if (geometryData.GeometryData == null)
            return;

        var textureHandle = geometryData.GeometryData.TextureHandle;

        if (!m_data.TryGetValue(textureHandle, out var list))
        {
            list = new();
            m_data[textureHandle] = list;
        }

        AssertDuplicate(list, geometryData);

        if (list.LastReleasedIndex != -1)
        {
            list.Geometry.Data[list.LastReleasedIndex] = new FreeGeometryData(textureHandle, geometryData);
            list.LastReleasedIndex = -1;
            return;
        }

        for (int i = 0; i < list.Geometry.Length; i++)
        {
            if (list.Geometry.Data[i].Released)
            {
                list.Geometry.Data[i] = new FreeGeometryData(textureHandle, geometryData);
                return;
            }
        }

        list.Geometry.Add(new FreeGeometryData(textureHandle, geometryData));
    }

    [Conditional("DEBUG")]
    private static void AssertDuplicate(FreeGeometryList list, in StaticGeometryData geometryData)
    {
        for (int i = 0; i < list.Geometry.Length; i++)
        {
            ref var item = ref list.Geometry.Data[i];
            if (item.Released)
                continue;

            if (item.Geometry.Index == geometryData.Index)
                Assert.Fail("GeometryData already added.");
        }
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

                if (itemData.Geometry.Length == vertexLength)
                    break;
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

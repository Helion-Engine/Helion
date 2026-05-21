using Helion.Util.Assertion;
using Helion.World.Static;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

readonly struct GeometryKey(int textureHandle, GeometryType type, bool repeatY) : IEquatable<GeometryKey>
{
    public readonly int TextureHandle = textureHandle;
    public readonly GeometryType Type = type;
    public readonly bool RepeatY = repeatY;

    public bool Equals(GeometryKey other)
    {
        return TextureHandle == other.TextureHandle && Type == other.Type && RepeatY == other.RepeatY;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TextureHandle, (int)Type, RepeatY);
    }

    public override bool Equals(object? obj)
    {
        return obj is GeometryKey key && Equals(key);
    }
}

public class FreeGeometryManager
{
    private readonly Dictionary<GeometryKey, FreeGeometryList> m_data = new(128);

    public void Add(in StaticGeometryData geometryData, GeometryType type, bool repeatY)
    {
        if (geometryData.GeometryData == null)
            return;

        var textureHandle = geometryData.GeometryData.TextureHandle;
        var key = new GeometryKey(textureHandle, type, repeatY);

        if (!m_data.TryGetValue(key, out var list))
        {
            list = new();
            m_data[key] = list;
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

    public bool GetAndRemove(int textureHandle, GeometryType type, bool repeatY, int vertexLength, [NotNullWhen(true)] out StaticGeometryData? data)
    {
        int minLength = int.MaxValue;
        int minIndex = -1;
        var key = new GeometryKey(textureHandle, type, repeatY);

        if (!m_data.TryGetValue(key, out var list))
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

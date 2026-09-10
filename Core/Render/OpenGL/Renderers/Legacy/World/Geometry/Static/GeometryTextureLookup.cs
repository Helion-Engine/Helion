using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class GeometryTextureLookup
{
    private readonly GeometryTypeLookup<TextureGeometryLookup> m_lookup = new(() => new TextureGeometryLookup());
    private readonly Dictionary<int, int> m_arrayTextureLookup = [];

    public void AddTextureArrayMap(int arrayTextureIndex, int textureIndex)
    {
        m_arrayTextureLookup[textureIndex] = arrayTextureIndex;
    }

    public void Clear()
    {
        var items = m_lookup.GetItems();
        for (int i = 0; i < items.Length; i++)
            items[i].Clear();
    }

    public bool TryGetValue(GeometryType type, int textureHandle, bool repeatY, [NotNullWhen(true)] out GeometryData? value)
    {
        if (m_arrayTextureLookup.TryGetValue(textureHandle, out var arrayTextureHandle))
            textureHandle = arrayTextureHandle;
        return m_lookup.Get(type).TryGetValue(textureHandle, repeatY, out value);
    }

    public void Add(GeometryType type, int textureHandle, bool repeatY, GeometryData data)
    {
        if (m_arrayTextureLookup.TryGetValue(textureHandle, out var arrayTextureHandle))
            textureHandle = arrayTextureHandle;
        m_lookup.Get(type).Add(textureHandle, repeatY, data);
    }
}

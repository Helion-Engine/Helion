using Helion.Render.OpenGL.Texture;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class GeometryTextureLookup(IArrayTextureLookup arrayTextureLookup)
{
    private readonly GeometryTypeLookup<TextureGeometryLookup> m_lookup = new(() => new TextureGeometryLookup());
    private readonly IArrayTextureLookup m_arrayTextureLookup = arrayTextureLookup;

    public void Clear()
    {
        var items = m_lookup.GetItems();
        for (int i = 0; i < items.Length; i++)
            items[i].Clear();
    }

    public bool TryGetValue(GeometryType type, int textureHandle, bool repeatY, [NotNullWhen(true)] out GeometryData? value)
    {
        textureHandle = m_arrayTextureLookup.GetArrayTextureHandle(textureHandle);
        return m_lookup.Get(type).TryGetValue(textureHandle, repeatY, out value);
    }

    public void Add(GeometryType type, int textureHandle, bool repeatY, GeometryData data)
    {
        textureHandle = m_arrayTextureLookup.GetArrayTextureHandle(textureHandle);
        m_lookup.Get(type).Add(textureHandle, repeatY, data);
    }
}

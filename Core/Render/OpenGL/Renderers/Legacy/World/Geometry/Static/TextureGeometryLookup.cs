using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

internal class TextureGeometryLookup
{
    private readonly Dictionary<int, GeometryData> m_textureToGeometryLookup = new();
    private readonly Dictionary<int, GeometryData> m_textureToGeometryLookupClamp = new();

    public void Clear()
    {
        m_textureToGeometryLookup.Clear();
        m_textureToGeometryLookupClamp.Clear();
    }

    public void Add(int textureHandle, bool repeat, GeometryData data)
    {
        GetLookup(repeat).Add(textureHandle, data);
    }

    public bool TryGetValue(int textureHanldle, bool repeat, [NotNullWhen(true)] out GeometryData? value) =>
        GetLookup(repeat).TryGetValue(textureHanldle, out value);

    private Dictionary<int, GeometryData> GetLookup(bool repeat) =>
        repeat ? m_textureToGeometryLookup : m_textureToGeometryLookupClamp;
}

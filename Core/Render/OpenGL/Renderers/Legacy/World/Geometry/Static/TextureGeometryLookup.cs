using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

internal class TextureGeometryLookup
{
    private readonly Dictionary<int, GeometryData> m_textureToGeometryLookup = [];
    private readonly Dictionary<int, GeometryData> m_textureToGeometryLookupClamp = [];

    public void Clear()
    {
        m_textureToGeometryLookup.Clear();
        m_textureToGeometryLookupClamp.Clear();
    }

    public void Add(int textureHandle, bool repeatY, GeometryData data)
    {
        GetLookup(repeatY).Add(textureHandle, data);
    }

    public bool TryGetValue(int textureHandle, bool repeatY, [NotNullWhen(true)] out GeometryData? value) =>
        GetLookup(repeatY).TryGetValue(textureHandle, out value);

    private Dictionary<int, GeometryData> GetLookup(bool repeatY) =>
        repeatY ? m_textureToGeometryLookup : m_textureToGeometryLookupClamp;
}

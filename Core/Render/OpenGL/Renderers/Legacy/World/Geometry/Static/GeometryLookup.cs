using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public enum GeometryType
{
    Wall,
    Flat
}

public class GeometryLookup
{
    private readonly TextureGeometryLookup m_wallTextureLookup = new();
    private readonly TextureGeometryLookup m_flatTextureLookup = new();

    public void Clear()
    {
        m_wallTextureLookup.Clear();
        m_flatTextureLookup.Clear();
    }

    public bool TryGetValue(GeometryType type, int textureHandle, bool repeatY, [NotNullWhen(true)] out GeometryData? value)
    {
        var lookup = GetLookup(type);
        return lookup.TryGetValue(textureHandle, repeatY, out value);
    }

    public void Add(GeometryType type, int textureHandle, bool repeatY, GeometryData data)
    {
        var lookup = GetLookup(type);
        lookup.Add(textureHandle, repeatY, data);
    }

    private TextureGeometryLookup GetLookup(GeometryType type)
    {
        if (type == GeometryType.Flat)
            return m_flatTextureLookup;
        return m_wallTextureLookup;
    }
}

using Helion.Graphics.Palettes;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Static;

namespace Helion.World.Geometry.Walls;

public sealed class Wall
{
    public readonly WallLocation Location;
    public int TextureHandle;

    public StaticGeometryData Static;
    private readonly int m_initialTextureHandle;

    public Wall(int textureHandle, WallLocation location)
    {
        TextureHandle = textureHandle;
        Location = location;
        m_initialTextureHandle = textureHandle;
    }

    public void Reset()
    {
        Static = default;
        TextureHandle = m_initialTextureHandle;
    }
}

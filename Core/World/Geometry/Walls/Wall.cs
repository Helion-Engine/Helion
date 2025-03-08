using Helion.Geometry.Vectors;
using Helion.World.Static;

namespace Helion.World.Geometry.Walls;

public sealed class Wall
{
    public readonly WallLocation Location;
    public int TextureHandle;
    public int LightLevel;
    public bool LightLevelAbsolute;

    public Vec2F Offset;
    public Vec2F Scale;

    public StaticGeometryData Static;
    private readonly int m_initialTextureHandle;

    public Wall(int textureHandle, WallLocation location)
        : this (textureHandle, location, 0, false, default, Vec2F.One)
    {

    }

    public Wall(int textureHandle, WallLocation location, int lightLevel, bool lightLevelAbsolute, Vec2F offset, Vec2F scale)
    {
        TextureHandle = textureHandle;
        Location = location;
        m_initialTextureHandle = textureHandle;
        LightLevel = lightLevel;
        LightLevelAbsolute = lightLevelAbsolute;
        Offset = offset;
        Scale = scale;
    }

    public void Reset()
    {
        Static = default;
        TextureHandle = m_initialTextureHandle;
    }
}

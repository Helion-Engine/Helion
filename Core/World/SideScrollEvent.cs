using Helion.World.Geometry.Sides;
using Helion.World.Static;

namespace Helion.World;

public readonly struct SideScrollEvent
{
    public readonly Side Side;
    public readonly SideTexture Textures;

    public SideScrollEvent(Side side, SideTexture textures)
    {
        Side = side;
        Textures = textures;
    }
}

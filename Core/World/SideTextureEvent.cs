using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.World;

public readonly struct SideTextureEvent
{
    public readonly Side Side;
    public readonly WallLocation Location;
    public readonly int TextureHandle;

    public SideTextureEvent(Side side, WallLocation location, int textureHandle)
    {
        Side = side;
        Location = location;
        TextureHandle = textureHandle;
    }
}

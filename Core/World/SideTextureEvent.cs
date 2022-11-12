using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.World;

public readonly struct SideTextureEvent
{
    public readonly Side Side;
    public readonly WallLocation Location;
    public readonly int TextureHandle;
    public readonly int PreviousTextureHandle;

    public SideTextureEvent(Side side, WallLocation location, int textureHandle, int previousTextureHandle)
    {
        Side = side;
        Location = location;
        TextureHandle = textureHandle;
        PreviousTextureHandle = previousTextureHandle;
    }
}

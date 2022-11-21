using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.World;

public readonly struct SideTextureEvent
{
    public readonly Side Side;
    public readonly Wall Wall;
    public readonly int TextureHandle;
    public readonly int PreviousTextureHandle;

    public SideTextureEvent(Side side, Wall wall, int textureHandle, int previousTextureHandle)
    {
        Side = side;
        Wall = wall;
        TextureHandle = textureHandle;
        PreviousTextureHandle = previousTextureHandle;
    }
}

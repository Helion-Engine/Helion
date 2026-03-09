using Helion.Geometry.Vectors;
using Helion.World.Geometry.Walls;

namespace Helion.Maps.Specials;

public enum ScrollOffsetType
{
    Current,
    Previous
}

public class SideScrollData
{
    public int Gametick;
    public Vec2D[] Offsets = new Vec2D[6];

    public unsafe ref Vec2D Offset(WallLocation location, ScrollOffsetType type)
    {
        return ref Offsets[(int)location + ((int)type * 3) - 1];
    }

    public unsafe ref Vec2D Offset(WallLocation location, bool previous)
    {
        int index = *(byte*)&previous;
        return ref Offsets[(int)location + (index * 3) - 1];
    }
}

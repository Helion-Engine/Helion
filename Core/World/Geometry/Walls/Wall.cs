using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;

namespace Helion.World.Geometry.Walls
{
    public class Wall
    {
        public readonly int Id;
        public readonly Side Side;
        public readonly WallLocation Location;
        public readonly Sector Ceiling;
        public readonly Sector Floor;
    }
}
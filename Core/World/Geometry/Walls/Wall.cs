using Helion.World.Geometry.Sides;

namespace Helion.World.Geometry.Walls
{
    public class Wall
    {
        public readonly int Id;
        public readonly WallLocation Location;
        public Side Side { get; internal set; }

        public Wall(int id, WallLocation location)
        {
            Id = id;
            Location = location;
        }
    }
}
using Helion.Util;
using Helion.World.Geometry.Sides;

namespace Helion.World.Geometry.Walls
{
    public class Wall
    {
        public readonly int Id;
        public readonly WallLocation Location;
        public Side Side { get; internal set; }
        public CIString Texture;

        public Wall(int id, CIString texture, WallLocation location)
        {
            Id = id;
            Texture = texture;
            Location = location;
        }
    }
}
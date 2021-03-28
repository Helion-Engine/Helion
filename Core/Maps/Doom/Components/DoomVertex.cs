using Helion.Geometry.Vectors;
using Helion.Maps.Components;

namespace Helion.Maps.Doom.Components
{
    public class DoomVertex : IVertex
    {
        public int Id { get; }
        public Vec2D Position { get; }
        public Vec2Fixed PositionFixed { get; }

        public DoomVertex(int id, Vec2Fixed positionFixed)
        {
            Id = id;
            Position = positionFixed.Double;
            PositionFixed = positionFixed;
        }
    }
}
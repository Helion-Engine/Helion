using Helion.MapsNew.Components;
using Helion.Util.Geometry;

namespace Helion.MapsNew.Doom.Components
{
    public class DoomVertex : IVertex
    {
        public int Id { get; }
        public Vec2D Position { get; }
        public Vec2Fixed PositionFixed { get; }

        public DoomVertex(int id, Vec2Fixed positionFixed)
        {
            Id = id;
            Position = positionFixed.ToDouble();
            PositionFixed = positionFixed;
        }
    }
}
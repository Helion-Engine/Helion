using Helion.Util.Geometry;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;

namespace Helion.World.Geometry.Sides
{
    public class Side
    {
        public readonly int Id;
        public readonly Sector Sector;
        public readonly Line Line;
        public readonly Vec2I Offset;
        public readonly Wall Middle;
        public Wall[] Walls { get; } = { };

        public bool IsFront => ReferenceEquals(this, Line.Front);
        public bool IsBack => !IsFront;
        public virtual Side? PartnerSide => Line.TwoSided ? (ReferenceEquals(this, Line.Front) ? Line.Back : Line.Front) : null;
    }
}
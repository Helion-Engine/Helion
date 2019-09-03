using Helion.Util.Geometry;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;

namespace Helion.World.Geometry.Sides
{
    public class Side
    {
        public readonly int Id;
        public readonly int MapId;
        public readonly Sector Sector;
        public readonly Vec2I Offset;
        public readonly Wall Middle;
        public Line Line { get; internal set; }
        public Wall[] Walls { get; protected set; }

        public bool IsFront => ReferenceEquals(this, Line.Front);
        public bool IsBack => !IsFront;
        public virtual Side? PartnerSide => Line.TwoSided ? (ReferenceEquals(this, Line.Front) ? Line.Back : Line.Front) : null;

        public Side(int id, int mapId, Vec2I offset, Wall middle, Sector sector)
        {
            Id = id;
            MapId = mapId;
            Sector = sector;
            Offset = offset;
            Middle = middle;
            Walls = new[] { middle };
            
            middle.Side = this;
            sector.Sides.Add(this);
        }
    }
}
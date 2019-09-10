using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
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
        public readonly Wall Middle;
        public Vec2I Offset;
        public Line Line { get; internal set; }
        public Wall[] Walls { get; protected set; }

        public bool IsFront => ReferenceEquals(this, Line.Front);

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
            
            // We are okay with things blowing up violently if someone forgets
            // to assign it, because that is such a critical error on the part
            // of the developer if this ever happens that it's deserved. Fixing
            // this would lead to some very messy logic, and when this is added
            // to a parent object, it will add itself for us. If this can be
            // fixed in the future with non-messy code, go for it.
            Line = null !;
        }
    }
}
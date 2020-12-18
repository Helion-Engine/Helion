using System.Collections.Generic;
using Helion.Maps.Specials;
using Helion.Util.Geometry.Vectors;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Geometry.Walls;

namespace Helion.Worlds.Geometry.Lines
{
    public class Side
    {
        public readonly int Id;
        public readonly Sector Sector;
        public readonly Wall? Upper;
        public readonly Wall? Middle;
        public readonly Wall? Lower;
        public Vec2I Offset;
        public Line Line { get; internal set; }
        public SideScrollData? ScrollData { get; set; }

        public bool IsFront => ReferenceEquals(this, Line.Front);
        public bool IsBack => !IsFront;
        public IEnumerable<Wall> Walls => CreateWallsEnumerable();

        public Side(int index, Vec2I offset, Wall middle, Sector sector) :
            this(index, offset, null, middle, null, sector)
        {
        }

        public Side(int id, Vec2I offset, Wall? upper, Wall? middle, Wall? lower, Sector sector)
        {
            Id = id;
            Sector = sector;
            Offset = offset;
            Upper = upper;
            Middle = middle;
            Lower = lower;

            if (upper != null)
                upper.Side = this;
            if (middle != null)
                middle.Side = this;
            if (lower != null)
                lower.Side = this;
            sector.Sides.Add(this);

            // We are okay with things blowing up violently if someone forgets
            // to assign it, because that is such a critical error on the part
            // of the developer if this ever happens that it's deserved. Fixing
            // this would lead to some very messy logic, and when this is added
            // to a parent object, it will add itself for us. If this can be
            // fixed in the future with non-messy code, go for it.
            Line = null!;
        }

        private IEnumerable<Wall> CreateWallsEnumerable()
        {
            if (Upper != null)
                yield return Upper;
            if (Middle != null)
                yield return Middle;
            if (Lower != null)
                yield return Lower;
        }
    }
}
using Helion.Util.Geometry;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;

namespace Helion.World.Geometry.Sides
{
    public class TwoSided : Side
    {
        public readonly Wall Upper;
        public readonly Wall Lower;

        public bool IsBack => !IsFront;
        public TwoSided PartnerSide => IsFront ? (TwoSided)Line.Back : (TwoSided)Line.Front;

        public TwoSided(int id, int mapId, Vec2I offset, Wall upper, Wall middle, Wall lower, Sector sector) : 
            base(id, mapId, offset, middle, sector)
        {
            Upper = upper;
            Lower = lower;
            Walls = new[] { middle, upper, lower };

            upper.Side = this;
            lower.Side = this;
        }
    }
}
using Helion.Geometry.Vectors;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;

namespace Helion.World.Geometry.Sides
{
    public class TwoSided : Side
    {
        public readonly Wall Upper;
        public readonly Wall Lower;

        public bool IsBack => !IsFront;
        public TwoSided PartnerSide => IsFront ? BackSide : (TwoSided)Line.Front;

        // This is to get around null references and IDE formatting/warnings
        // not playing nicely together (yet).
        private TwoSided BackSide => (TwoSided)Line.Back !;

        public TwoSided(int id, Vec2I offset, Wall upper, Wall middle, Wall lower, Sector sector) : 
            base(id, offset, middle, sector)
        {
            Upper = upper;
            Lower = lower;
            Walls = new[] { middle, upper, lower };

            upper.Side = this;
            lower.Side = this;
        }
    }
}
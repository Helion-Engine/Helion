using Helion.Geometry.Vectors;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;

namespace Helion.World.Geometry.Sides
{
    public class TwoSided : Side
    {
        public bool IsBack => !IsFront;
        public TwoSided PartnerSide => IsFront ? BackSide : (TwoSided)Line.Front;

        // This is to get around null references and IDE formatting/warnings
        // not playing nicely together (yet).
        private TwoSided BackSide => (TwoSided)Line.Back !;

        public TwoSided(int id, Vec2I offset, Wall upper, Wall middle, Wall lower, Sector sector) : 
            base(id, offset, upper, middle, lower,  sector)
        {
        }
    }
}
using Helion.World.Geometry.Walls;

namespace Helion.World.Geometry.Sides
{
    public class TwoSided : Side
    {
        public readonly Wall Upper;
        public readonly Wall Lower;
        
        public override Side PartnerSide => Line.Front;
    }
}
using Helion.Geometry.Vectors;

namespace Helion.Maps.Specials
{
    public class SideScrollData
    {
        public const int UpperPosition = 0;
        public const int MiddlePosition = 1;
        public const int LowerPosition = 2;

        public Vec2D[] LastOffset = new Vec2D[3];
        public Vec2D[] Offset = new Vec2D[3];
    }
}

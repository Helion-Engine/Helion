using Helion.Geometry.Vectors;

namespace Helion.Geometry.Boxes
{
    public struct Box3F
    {
        public Vec3F Min;
        public Vec3F Max;

        public Box3F(Vec3F min, Vec3F max)
        {
            Min = min;
            Max = max;
        }
    }
}

using Helion.Geometry.Vectors;

namespace Helion.Geometry.Boxes
{
    public struct Box2F
    {
        public Vec2F Min;
        public Vec2F Max;
        
        public Vec2F TopLeft => (Min.X, Max.Y);
        public Vec2F BottomLeft => Min;
        public Vec2F BottomRight => (Max.X, Min.Y);
        public Vec2F TopRight => Max;

        public Box2F(Vec2F min, Vec2F max)
        {
            Min = min;
            Max = max;
        }
        
        public bool Overlaps(in Box2F box)
        {
            return !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        }
    }
}

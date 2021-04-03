using Helion.Geometry.Vectors;

namespace Helion.Geometry.BoxesNew
{
    public class BoundingBox2D
    {
        public readonly Vec2D Min;
        public readonly Vec2D Max;

        public Vec2D TopLeft => new(Min.X, Max.Y);
        public Vec2D BottomLeft => Min;
        public Vec2D BottomRight => new(Max.X, Min.Y);
        public Vec2D TopRight => Max;
        public double Top => Max.Y;
        public double Bottom => Min.Y;
        public double Left => Min.X;
        public double Right => Max.X;
        public double Width => Max.X - Min.X;
        public double Height => Max.Y - Min.Y;
    }
}

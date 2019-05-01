using System.Numerics;

namespace Helion.Util.Geometry
{
    public struct Box2f
    {
        public Vector2 Min;
        public Vector2 Max;
        public Vector2 TopLeft => new Vector2(Min.X, Max.Y);
        public Vector2 BottomRight => new Vector2(Max.X, Min.Y);
        public Vector2 BottomLeft => Min;
        public Vector2 TopRight => Max;

        public Box2f(Vector2 min, Vector2 max)
        {
            Assert.Precondition(min.X <= max.X, "Bounding box min X > max X");
            Assert.Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max;
        }

        public bool Contains(Vector2 point)
        {
            return (Min.X < point.X && point.X < Max.X) && (Min.Y < point.Y && point.Y < Max.Y);
        }

        public bool Contains(Vector3 point)
        {
            return (Min.X < point.X && point.X < Max.X) && (Min.Y < point.Y && point.Y < Max.Y);
        }

        public bool Overlaps(Box2f box)
        {
            return !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        }

        public Vector2 Sides() => Max - Min;

        public override string ToString() => $"({Min}), ({Max})";
    }
}

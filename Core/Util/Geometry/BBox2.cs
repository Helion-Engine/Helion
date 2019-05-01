using System.Numerics;

namespace Helion.Util.Geometry
{
    public struct BBox2f
    {
        public Vector2 Min;
        public Vector2 Max;

        public BBox2f(Vector2 min, Vector2 max)
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

        public bool Overlaps(BBox2f box)
        {
            return !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        }

        public Vector2 Sides()
        {
            return Max - Min;
        }

        public override string ToString() 
        {
            return $"({Min}), ({Max})";
        }
    }
}

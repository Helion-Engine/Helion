// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Boxes
{
    public class BoundingBox2D
    {
        protected Vec2D m_Min;
        protected Vec2D m_Max;

        public Vec2D Min => m_Min;
        public Vec2D Max => m_Max;
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
        public Box2D Struct => new(Min, Max);
        public Vec2D Sides => Max - Min;

        public BoundingBox2D(Vec2D min, Vec2D max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min;
            m_Max = max;
        }

        public BoundingBox2D(Vec2D min, Vector2D max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min;
            m_Max = max.Struct;
        }

        public BoundingBox2D(Vector2D min, Vec2D max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min.Struct;
            m_Max = max;
        }

        public BoundingBox2D(Vector2D min, Vector2D max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min.Struct;
            m_Max = max.Struct;
        }

        public BoundingBox2D(Vec2D center, double radius)
        {
            Precondition(radius >= 0, "Bounding box radius yields min X > max X");
            m_Min = new(center.X - radius, center.Y - radius);
            m_Max = new(center.X + radius, center.Y + radius);
        }

        public BoundingBox2D(Vector2D center, double radius)
        {
            Precondition(radius >= 0, "Bounding box radius yields min X > max X");
            m_Min = new(center.X - radius, center.Y - radius);
            m_Max = new(center.X + radius, center.Y + radius);
        }

        public void Deconstruct(out Vec2D min, out Vec2D max)
        {
            min = Min;
            max = Max;
        }

        public static Box2D operator +(BoundingBox2D self, Vec2D offset) => new(self.Min + offset, self.Max + offset);
        public static Box2D operator +(BoundingBox2D self, Vector2D offset) => new(self.Min + offset, self.Max + offset);
        public static Box2D operator -(BoundingBox2D self, Vec2D offset) => new(self.Min - offset, self.Max - offset);
        public static Box2D operator -(BoundingBox2D self, Vector2D offset) => new(self.Min - offset, self.Max - offset);

        public bool Contains(Vec2D point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vector2D point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vec3D point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vector3D point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Overlaps(in Box2D box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        public bool Overlaps(BoundingBox2D box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        public bool Intersects(Seg2D seg) => seg.Intersects(this);
        public bool Intersects(Segment2D seg) => seg.Intersects(this);
        public bool Intersects<T>(SegmentT2D<T> seg) where T : Vector2D => seg.Intersects(this);
        public Seg2D GetSpanningEdge(Vector2D position) => GetSpanningEdge(position.Struct);
        public Seg2D GetSpanningEdge(Vec2D position)
            {
                // This is best understood by asking ourselves how we'd classify
                // where we are along a 1D line. Suppose we want to find out which
                // one of the spans were in along the X axis:
                //
                //      0     1     2
                //   A-----B-----C-----D
                //
                // We want to know if we're in span 0, 1, or 2. We can just check
                // by doing `if x > B` for span 1 or 2, and `if x > C` for span 2.
                // Instead of doing if statements, we can just cast the bool to an
                // int and add them up.
                //
                // Next we do this along the Y axis.
                //
                // After our results, we can merge the bits such that the higher
                // two bits are the Y value, and the lower 2 bits are the X value.
                // This gives us: 0bYYXX.
                //
                // Since each coordinate in the following image has its own unique
                // bitcode, we can switch on the bitcode to get the corners.
                //
                //       XY values           Binary codes
                //
                //      02 | 12 | 22       1000|1001|1010
                //         |    |           8  | 9  | A
                //     ----o----o----      ----o----o----
                //      01 | 11 | 21       0100|0101|0110
                //         |    |           4  | 5  | 6
                //     ----o----o----      ----o----o----
                //      00 | 10 | 20       0000|0001|0010
                //         |    |           0  | 1  | 2
                //
                // Note this is my optimization to the Cohen-Sutherland algorithm
                // bitcode detector.
                uint horizontalBits = Convert.ToUInt32(position.X > Left) + Convert.ToUInt32(position.X > Right);
                uint verticalBits = Convert.ToUInt32(position.Y > Bottom) + Convert.ToUInt32(position.Y > Top);

                switch (horizontalBits | (verticalBits << 2))
                {
                case 0x0: // Bottom left
                    return (TopLeft, BottomRight);
                case 0x1: // Bottom middle
                    return (BottomLeft, BottomRight);
                case 0x2: // Bottom right
                    return (BottomLeft, TopRight);
                case 0x4: // Middle left
                    return (TopLeft, BottomLeft);
                case 0x5: // Center (this shouldn't be a case via precondition).
                    return (TopLeft, BottomRight);
                case 0x6: // Middle right
                    return (BottomRight, TopRight);
                case 0x8: // Top left
                    return (TopRight, BottomLeft);
                case 0x9: // Top middle
                    return (TopRight, TopLeft);
                case 0xA: // Top right
                    return (BottomRight, TopLeft);
                default:
                    Fail("Unexpected spanning edge bit code");
                    return (TopLeft, BottomRight);
                }
            }

        public Box2D Combine(params Box2D[] boxes)
        {
            Vec2D min = Min;
            Vec2D max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                Box2D box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
            }
            return new(min, max);
        }
        public Box2D Combine(params BoundingBox2D[] boxes)
        {
            Vec2D min = Min;
            Vec2D max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                BoundingBox2D box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
            }
            return new(min, max);
        }
        public override string ToString() => $"({Min}), ({Max})";
    }
}

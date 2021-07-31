// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Boxes
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public readonly struct Box2F
    {
        public static readonly Box2F UnitBox = ((0, 0), (1, 1));

        public readonly Vec2F Min;
        public readonly Vec2F Max;

        public Vec2F TopLeft => new(Min.X, Max.Y);
        public Vec2F BottomLeft => Min;
        public Vec2F BottomRight => new(Max.X, Min.Y);
        public Vec2F TopRight => Max;
        public float Top => Max.Y;
        public float Bottom => Min.Y;
        public float Left => Min.X;
        public float Right => Max.X;
        public float Width => Max.X - Min.X;
        public float Height => Max.Y - Min.Y;
        public Vec2F Sides => Max - Min;

        public Box2F(Vec2F min, Vec2F max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max;
        }

        public Box2F(Vec2F min, Vector2F max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max.Struct;
        }

        public Box2F(Vector2F min, Vec2F max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min.Struct;
            Max = max;
        }

        public Box2F(Vector2F min, Vector2F max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min.Struct;
            Max = max.Struct;
        }

        public Box2F(Vec2F center, float radius)
        {
            Precondition(radius >= 0, "Bounding box radius yields min X > max X");

            Min = new(center.X - radius, center.Y - radius);
            Max = new(center.X + radius, center.Y + radius);
        }

        public Box2F(Vector2F center, float radius)
        {
            Precondition(radius >= 0, "Bounding box radius yields min X > max X");

            Min = new(center.X - radius, center.Y - radius);
            Max = new(center.X + radius, center.Y + radius);
        }

        public static implicit operator Box2F(ValueTuple<float, float, float, float> tuple)
        {
            return new((tuple.Item1, tuple.Item2), (tuple.Item3, tuple.Item4));
        }

        public static implicit operator Box2F(ValueTuple<Vec2F, Vec2F> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box2F(ValueTuple<Vec2F, Vector2F> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box2F(ValueTuple<Vector2F, Vec2F> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box2F(ValueTuple<Vector2F, Vector2F> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out Vec2F min, out Vec2F max)
        {
            min = Min;
            max = Max;
        }

        public static Box2F operator +(Box2F self, Vec2F offset) => new(self.Min + offset, self.Max + offset);
        public static Box2F operator +(Box2F self, Vector2F offset) => new(self.Min + offset, self.Max + offset);
        public static Box2F operator -(Box2F self, Vec2F offset) => new(self.Min - offset, self.Max - offset);
        public static Box2F operator -(Box2F self, Vector2F offset) => new(self.Min - offset, self.Max - offset);

        public bool Contains(Vec2F point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vector2F point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vec3F point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vector3F point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Overlaps(in Box2F box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        public bool Overlaps(BoundingBox2F box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        public bool Intersects(Seg2F seg) => seg.Intersects(this);
        public bool Intersects(Segment2F seg) => seg.Intersects(this);
        public bool Intersects<T>(SegmentT2F<T> seg) where T : Vector2F => seg.Intersects(this);
        public Seg2F GetSpanningEdge(Vector2F position) => GetSpanningEdge(position.Struct);
        public Seg2F GetSpanningEdge(Vec2F position)
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

        public Box2F Combine(params Box2F[] boxes)
        {
            Vec2F min = Min;
            Vec2F max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                Box2F box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
            }
            return new(min, max);
        }
        public Box2F Combine(params BoundingBox2F[] boxes)
        {
            Vec2F min = Min;
            Vec2F max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                BoundingBox2F box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
            }
            return new(min, max);
        }
        public static Box2F? Combine(IEnumerable<Box2F> items) 
        {
            if (items.Empty())
                return null;
            Box2F initial = items.First();
            return items.Skip(1).Aggregate(initial, (acc, box) =>
            {
                Vec2F min = acc.Min;
                Vec2F max = acc.Max;
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
                return new Box2F(min, max);
            }
            );
        }
        public static Box2F? Combine(IEnumerable<BoundingBox2F> items) 
        {
            if (items.Empty())
                return null;
            Box2F initial = items.First().Struct;
            return items.Skip(1).Select(s => s.Struct).Aggregate(initial, (acc, box) =>
            {
                Vec2F min = acc.Min;
                Vec2F max = acc.Max;
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
                return new Box2F(min, max);
            }
            );
        }
        public static Box2F? Bound(IEnumerable<Seg2F> items) 
        {
            if (items.Empty())
                return null;
            Box2F initial = items.First().Box;
            return items.Skip(1).Select(s => s.Box).Aggregate(initial, (acc, box) =>
            {
                Vec2F min = acc.Min;
                Vec2F max = acc.Max;
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
                return new Box2F(min, max);
            }
            );
        }
        public static Box2F? Bound(IEnumerable<Segment2F> items) 
        {
            if (items.Empty())
                return null;
            Box2F initial = items.First().Box;
            return items.Skip(1).Select(s => s.Box).Aggregate(initial, (acc, box) =>
            {
                Vec2F min = acc.Min;
                Vec2F max = acc.Max;
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
                return new Box2F(min, max);
            }
            );
        }
        public static Box2F? Bound<T>(IEnumerable<SegmentT2F<T>> items) where T : Vector2F
        {
            if (items.Empty())
                return null;
            Box2F initial = items.First().Box;
            return items.Skip(1).Select(s => s.Box).Aggregate(initial, (acc, box) =>
            {
                Vec2F min = acc.Min;
                Vec2F max = acc.Max;
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
                return new Box2F(min, max);
            }
            );
        }
        public override string ToString() => $"({Min}), ({Max})";
    }
}

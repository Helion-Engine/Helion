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
    public readonly struct Box2Fixed
    {
        public readonly Vec2Fixed Min;
        public readonly Vec2Fixed Max;

        public Vec2Fixed TopLeft => new(Min.X, Max.Y);
        public Vec2Fixed BottomLeft => Min;
        public Vec2Fixed BottomRight => new(Max.X, Min.Y);
        public Vec2Fixed TopRight => Max;
        public Fixed Top => Max.Y;
        public Fixed Bottom => Min.Y;
        public Fixed Left => Min.X;
        public Fixed Right => Max.X;
        public Fixed Width => Max.X - Min.X;
        public Fixed Height => Max.Y - Min.Y;
        public Vec2Fixed Sides => Max - Min;

        public Box2Fixed(Vec2Fixed min, Vec2Fixed max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max;
        }

        public Box2Fixed(Vec2Fixed min, Vector2Fixed max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max.Struct;
        }

        public Box2Fixed(Vector2Fixed min, Vec2Fixed max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min.Struct;
            Max = max;
        }

        public Box2Fixed(Vector2Fixed min, Vector2Fixed max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min.Struct;
            Max = max.Struct;
        }

        public Box2Fixed(Vec2Fixed center, Fixed radius)
        {
            Precondition(radius >= 0, "Bounding box radius yields min X > max X");

            Min = new(center.X - radius, center.Y - radius);
            Max = new(center.X + radius, center.Y + radius);
        }

        public Box2Fixed(Vector2Fixed center, Fixed radius)
        {
            Precondition(radius >= 0, "Bounding box radius yields min X > max X");

            Min = new(center.X - radius, center.Y - radius);
            Max = new(center.X + radius, center.Y + radius);
        }

        public static implicit operator Box2Fixed(ValueTuple<Fixed, Fixed, Fixed, Fixed> tuple)
        {
            return new((tuple.Item1, tuple.Item2), (tuple.Item3, tuple.Item4));
        }

        public static implicit operator Box2Fixed(ValueTuple<Vec2Fixed, Vec2Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box2Fixed(ValueTuple<Vec2Fixed, Vector2Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box2Fixed(ValueTuple<Vector2Fixed, Vec2Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box2Fixed(ValueTuple<Vector2Fixed, Vector2Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out Vec2Fixed min, out Vec2Fixed max)
        {
            min = Min;
            max = Max;
        }

        public static Box2Fixed operator +(Box2Fixed self, Vec2Fixed offset) => new(self.Min + offset, self.Max + offset);
        public static Box2Fixed operator +(Box2Fixed self, Vector2Fixed offset) => new(self.Min + offset, self.Max + offset);
        public static Box2Fixed operator -(Box2Fixed self, Vec2Fixed offset) => new(self.Min - offset, self.Max - offset);
        public static Box2Fixed operator -(Box2Fixed self, Vector2Fixed offset) => new(self.Min - offset, self.Max - offset);

        public bool Contains(Vec2Fixed point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vector2Fixed point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vec3Fixed point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vector3Fixed point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Overlaps(in Box2Fixed box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        public bool Overlaps(BoundingBox2Fixed box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        public Box2Fixed Combine(params Box2Fixed[] boxes)
        {
            Vec2Fixed min = Min;
            Vec2Fixed max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                Box2Fixed box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
            }
            return new(min, max);
        }
        public Box2Fixed Combine(params BoundingBox2Fixed[] boxes)
        {
            Vec2Fixed min = Min;
            Vec2Fixed max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                BoundingBox2Fixed box = boxes[i];
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

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

namespace Helion.Geometry.Boxes;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct Box3F
{
    public static readonly Box3F UnitBox = ((0, 0, 0), (1, 1, 1));

    public readonly Vec3F Min;
    public readonly Vec3F Max;

    public Box3I Int => new(Min.Int, Max.Int);
    public Box3D Double => new(Min.Double, Max.Double);
    public Vec3F Sides => Max - Min;

    public Box3F(Vec3F min, Vec3F max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        Min = min;
        Max = max;
    }

    public Box3F(Vec3F min, Vector3F max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        Min = min;
        Max = max.Struct;
    }

    public Box3F(Vector3F min, Vec3F max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        Min = min.Struct;
        Max = max;
    }

    public Box3F(Vector3F min, Vector3F max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        Min = min.Struct;
        Max = max.Struct;
    }

    public static implicit operator Box3F(ValueTuple<Vec3F, Vec3F> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static implicit operator Box3F(ValueTuple<Vec3F, Vector3F> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static implicit operator Box3F(ValueTuple<Vector3F, Vec3F> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static implicit operator Box3F(ValueTuple<Vector3F, Vector3F> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out Vec3F min, out Vec3F max)
    {
        min = Min;
        max = Max;
    }

    public static Box3F operator *(Box3F self, float scale) => new(self.Min * scale, self.Max * scale);
    public static Box3F operator /(Box3F self, float divisor) => new(self.Min / divisor, self.Max / divisor);
    public static Box3F operator +(Box3F self, Vec3F offset) => new(self.Min + offset, self.Max + offset);
    public static Box3F operator +(Box3F self, Vector3F offset) => new(self.Min + offset, self.Max + offset);
    public static Box3F operator -(Box3F self, Vec3F offset) => new(self.Min - offset, self.Max - offset);
    public static Box3F operator -(Box3F self, Vector3F offset) => new(self.Min - offset, self.Max - offset);

    public bool Contains(Vec3F point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
    public bool Contains(Vector3F point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
    public bool Overlaps2D(in Box2F other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps2D(in Box3F other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps2D(BoundingBox2F other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps2D(BoundingBox3F other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps(in Box3F box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
    public bool Overlaps(BoundingBox3F box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
    public Box3F Combine(params Box3F[] boxes)
    {
        Vec3F min = Min;
        Vec3F max = Max;
        for (int i = 0; i < boxes.Length; i++)
        {
            Box3F box = boxes[i];
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
        }
        return new(min, max);
    }
    public Box3F Combine(params BoundingBox3F[] boxes)
    {
        Vec3F min = Min;
        Vec3F max = Max;
        for (int i = 0; i < boxes.Length; i++)
        {
            BoundingBox3F box = boxes[i];
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
        }
        return new(min, max);
    }
    public static Box3F? Combine(IEnumerable<Box3F> items)
    {
        if (items.Empty())
            return null;
        Box3F initial = items.First();
        return items.Skip(1).Aggregate(initial, (acc, box) =>
        {
            Vec3F min = acc.Min;
            Vec3F max = acc.Max;
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
            return new Box3F(min, max);
        }
        );
    }
    public static Box3F? Combine(IEnumerable<BoundingBox3F> items)
    {
        if (items.Empty())
            return null;
        Box3F initial = items.First().Struct;
        return items.Skip(1).Select(s => s.Struct).Aggregate(initial, (acc, box) =>
        {
            Vec3F min = acc.Min;
            Vec3F max = acc.Max;
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
            return new Box3F(min, max);
        }
        );
    }
    public static Box3F? Bound(IEnumerable<Seg3F> items)
    {
        if (items.Empty())
            return null;
        Box3F initial = items.First().Box;
        return items.Skip(1).Select(s => s.Box).Aggregate(initial, (acc, box) =>
        {
            Vec3F min = acc.Min;
            Vec3F max = acc.Max;
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
            return new Box3F(min, max);
        }
        );
    }
    public static Box3F? Bound(IEnumerable<Segment3F> items)
    {
        if (items.Empty())
            return null;
        Box3F initial = items.First().Box;
        return items.Skip(1).Select(s => s.Box).Aggregate(initial, (acc, box) =>
        {
            Vec3F min = acc.Min;
            Vec3F max = acc.Max;
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
            return new Box3F(min, max);
        }
        );
    }
    public static Box3F? Bound<T>(IEnumerable<SegmentT3F<T>> items) where T : Vector3F
    {
        if (items.Empty())
            return null;
        Box3F initial = items.First().Box;
        return items.Skip(1).Select(s => s.Box).Aggregate(initial, (acc, box) =>
        {
            Vec3F min = acc.Min;
            Vec3F max = acc.Max;
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
            return new Box3F(min, max);
        }
        );
    }
    public override string ToString() => $"({Min}), ({Max})";
}

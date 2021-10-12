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

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public readonly struct Box3D
{
    public static readonly Box3D UnitBox = ((0, 0, 0), (1, 1, 1));

    public readonly Vec3D Min;
    public readonly Vec3D Max;

    public Box3I Int => new(Min.Int, Max.Int);
    public Box3F Float => new(Min.Float, Max.Float);
    public Vec3D Sides => Max - Min;

    public Box3D(Vec3D min, Vec3D max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        Min = min;
        Max = max;
    }

    public Box3D(Vec3D min, Vector3D max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        Min = min;
        Max = max.Struct;
    }

    public Box3D(Vector3D min, Vec3D max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        Min = min.Struct;
        Max = max;
    }

    public Box3D(Vector3D min, Vector3D max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        Min = min.Struct;
        Max = max.Struct;
    }

    public static implicit operator Box3D(ValueTuple<Vec3D, Vec3D> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static implicit operator Box3D(ValueTuple<Vec3D, Vector3D> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static implicit operator Box3D(ValueTuple<Vector3D, Vec3D> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static implicit operator Box3D(ValueTuple<Vector3D, Vector3D> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out Vec3D min, out Vec3D max)
    {
        min = Min;
        max = Max;
    }

    public static Box3D operator *(Box3D self, double scale) => new(self.Min * scale, self.Max * scale);
    public static Box3D operator /(Box3D self, double divisor) => new(self.Min / divisor, self.Max / divisor);
    public static Box3D operator +(Box3D self, Vec3D offset) => new(self.Min + offset, self.Max + offset);
    public static Box3D operator +(Box3D self, Vector3D offset) => new(self.Min + offset, self.Max + offset);
    public static Box3D operator -(Box3D self, Vec3D offset) => new(self.Min - offset, self.Max - offset);
    public static Box3D operator -(Box3D self, Vector3D offset) => new(self.Min - offset, self.Max - offset);

    public bool Contains(Vec3D point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
    public bool Contains(Vector3D point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
    public bool Overlaps2D(in Box2D other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps2D(in Box3D other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps2D(BoundingBox2D other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps2D(BoundingBox3D other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps(in Box3D box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
    public bool Overlaps(BoundingBox3D box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
    public Box3D Combine(params Box3D[] boxes)
    {
        Vec3D min = Min;
        Vec3D max = Max;
        for (int i = 0; i < boxes.Length; i++)
        {
            Box3D box = boxes[i];
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
        }
        return new(min, max);
    }
    public Box3D Combine(params BoundingBox3D[] boxes)
    {
        Vec3D min = Min;
        Vec3D max = Max;
        for (int i = 0; i < boxes.Length; i++)
        {
            BoundingBox3D box = boxes[i];
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
        }
        return new(min, max);
    }
    public static Box3D? Combine(IEnumerable<Box3D> items)
    {
        if (items.Empty())
            return null;
        Box3D initial = items.First();
        return items.Skip(1).Aggregate(initial, (acc, box) =>
        {
            Vec3D min = acc.Min;
            Vec3D max = acc.Max;
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
            return new Box3D(min, max);
        }
        );
    }
    public static Box3D? Combine(IEnumerable<BoundingBox3D> items)
    {
        if (items.Empty())
            return null;
        Box3D initial = items.First().Struct;
        return items.Skip(1).Select(s => s.Struct).Aggregate(initial, (acc, box) =>
        {
            Vec3D min = acc.Min;
            Vec3D max = acc.Max;
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
            return new Box3D(min, max);
        }
        );
    }
    public static Box3D? Bound(IEnumerable<Seg3D> items)
    {
        if (items.Empty())
            return null;
        Box3D initial = items.First().Box;
        return items.Skip(1).Select(s => s.Box).Aggregate(initial, (acc, box) =>
        {
            Vec3D min = acc.Min;
            Vec3D max = acc.Max;
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
            return new Box3D(min, max);
        }
        );
    }
    public static Box3D? Bound(IEnumerable<Segment3D> items)
    {
        if (items.Empty())
            return null;
        Box3D initial = items.First().Box;
        return items.Skip(1).Select(s => s.Box).Aggregate(initial, (acc, box) =>
        {
            Vec3D min = acc.Min;
            Vec3D max = acc.Max;
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
            return new Box3D(min, max);
        }
        );
    }
    public static Box3D? Bound<T>(IEnumerable<SegmentT3D<T>> items) where T : Vector3D
    {
        if (items.Empty())
            return null;
        Box3D initial = items.First().Box;
        return items.Skip(1).Select(s => s.Box).Aggregate(initial, (acc, box) =>
        {
            Vec3D min = acc.Min;
            Vec3D max = acc.Max;
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
            return new Box3D(min, max);
        }
        );
    }
    public override string ToString() => $"({Min}), ({Max})";
}

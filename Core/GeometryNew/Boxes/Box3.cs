using System;
using Helion.GeometryNew.Vectors;
using Helion.Util.Extensions;

namespace Helion.GeometryNew.Boxes;

public struct Box3
{
    public Vec3 Min;
    public Vec3 Max;

    public Vec3 Sides => Max - Min;
    public Vec3 Extent => Sides * 0.5f;
    public Vec3 Center => Min + Extent;

    public Box3(Vec3 min, Vec3 max)
    {
        Min = min;
        Max = max;
    }

    public static implicit operator Box3(ValueTuple<Vec3, Vec3> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }
    
    public static Box3 operator +(Box3 self, Vec3 offset) => (self.Min + offset, self.Max + offset);
    public static Box3 operator -(Box3 self, Vec3 offset) => (self.Min - offset, self.Max - offset);
    public static Box3 operator *(Box3 self, float scale) => (self.Min * scale, self.Max * scale);
    public static Box3 operator /(Box3 self, float divisor) => (self.Min / divisor, self.Max / divisor);
    
    public Box3 Bound(Box3 box) => (Min.Min(box.Min), Max.Max(box.Max));
    public bool Contains(Vec3 p) => p.X > Min.X && p.X < Max.X && p.Y > Min.Y && p.Y < Max.Y && p.Z > Min.Z && p.Y < Max.Z;
    public bool Intersects(Box3 box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
    
    public override string ToString() => $"({Min}), ({Max})";
}
using System;
using Helion.GeometryNew.Vectors;
using Helion.Util.Extensions;

namespace Helion.GeometryNew.Boxes;

public struct Box2
{
    public Vec2 Min;
    public Vec2 Max;
    
    public float Top => Max.Y;
    public float Bottom => Min.Y;
    public float Left => Min.X;
    public float Right => Max.X;
    public Vec2 TopLeft => (Left, Top);
    public Vec2 BottomLeft => Min;
    public Vec2 BottomRight => (Right, Bottom);
    public Vec2 TopRight => Max;
    public float Width => Right - Left;
    public float Height => Top - Bottom;
    public Vec2 Sides => Max - Min;
    public Vec2 Extent => Sides * 0.5f;
    public Vec2 Center => Min + Extent;

    public Box2(Vec2 min, Vec2 max)
    {
        Min = min;
        Max = max;
    }
    
    public static implicit operator Box2(ValueTuple<float, float, float, float> tuple)
    {
        return new((tuple.Item1, tuple.Item2), (tuple.Item3, tuple.Item4));
    }

    public static implicit operator Box2(ValueTuple<Vec2, Vec2> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }
    
    public static Box2 operator +(Box2 self, Vec2 offset) => (self.Min + offset, self.Max + offset);
    public static Box2 operator -(Box2 self, Vec2 offset) => (self.Min - offset, self.Max - offset);
    public static Box2 operator *(Box2 self, float scale) => (self.Min * scale, self.Max * scale);
    public static Box2 operator /(Box2 self, float divisor) => (self.Min / divisor, self.Max / divisor);
    
    public Vec2 Clamp(Vec2 point) => (point.X.Clamp(Left, Right), point.Y.Clamp(Bottom, Top));
    public bool Contains(Vec2 point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
    public bool Contains(Vec3 point) => Contains(point.XY);
    public bool Intersects(Box2 box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
    
    public override string ToString() => $"({Min}), ({Max})";
}
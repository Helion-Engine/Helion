using System;
using Helion.GeometryNew.Boxes;
using Helion.GeometryNew.Segments;
using Helion.GeometryNew.Vectors;
using Helion.Util.Extensions;

namespace Helion.GeometryNew.Circles;

public readonly record struct Circle(Vec2 Origin, float Radius)
{
    public Circle(float radius) : this((0, 0), radius)
    {
    }
    
    public Circle(Vec2 origin, Vec2 edge) : this(origin, origin.Distance(edge))
    {
    }

    public static Vec2 UnitPoint(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));
    
    public Vec2 Point(float radians) => (UnitPoint(radians) * Radius) + Origin;
    public bool Contains(Vec2 point) => Origin.Distance(point) < Radius;
    public bool Contains(Vec3 point) => Contains(point.XY);

    public bool Intersects(Circle circle)
    {
        // Source: https://www.petercollingridge.co.uk/tutorials/computational-geometry/circle-circle-intersections/
        float originDist = circle.Radius + Radius;
        return (circle.Origin - Origin).LengthSquared < originDist * originDist;
    }

    public bool Intersects(Box2 box)
    {
        // Source: https://stackoverflow.com/a/1879223/3453041
        Vec2 closest = (Origin.X.Clamp(box.Left, box.Right), Origin.Y.Clamp(box.Top, box.Bottom));
        Vec2 distance = Origin - closest;
        return distance.LengthSquared < (Radius * Radius);
    }
    
    public bool Intersects(Seg2 seg)
    {
        // TODO
        throw new NotImplementedException();
    }
    
    public override string ToString() => $"({Origin}), {Radius}";
    public override int GetHashCode() => HashCode.Combine(Origin.GetHashCode(), Radius);
}
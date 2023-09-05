using System;
using Helion.GeometryNew.Circles;
using Helion.GeometryNew.Segments;
using Helion.GeometryNew.Vectors;

namespace Helion.GeometryNew.Rays;

public readonly record struct Ray2(Vec2 Origin, Vec2 Direction)
{
    public Vec2 PointAt(float t) => Origin + (Direction * t);
    public Seg2 FromLength(float length) => (Origin, Direction * length);

    public bool Intersect(Circle circle, out float t0, out float t1)
    {
        t0 = float.NaN;
        t1 = float.NaN;

        // Source: http://kylehalladay.com/blog/tutorial/math/2013/12/24/Ray-Sphere-Intersection.html
        // Modified, hoping dimensionality reduction changes nothing.
        Vec2 delta = circle.Origin - Origin;
        float tCenter = delta.Dot(Direction);
        if (tCenter < 0)
            return false;
        
        float dSquared = (tCenter * tCenter) - delta.Dot(delta);
        float radiusSquared = circle.Radius * circle.Radius;
        if (dSquared > radiusSquared)
            return false;

        float t1Center = MathF.Sqrt(radiusSquared - dSquared);
        t0 = tCenter - t1Center;
        t1 = tCenter + t1Center;

        if (t0 > t1)
            (t0, t1) = (t1, t0);
        
        return true;
    }
}
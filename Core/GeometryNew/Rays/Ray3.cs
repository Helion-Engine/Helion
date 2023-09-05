using System;
using Helion.GeometryNew.Boxes;
using Helion.GeometryNew.Circles;
using Helion.GeometryNew.Triangles;
using Helion.GeometryNew.Vectors;
using Helion.Util.Extensions;

namespace Helion.GeometryNew.Rays;

// Direction is a unit vector.
public readonly record struct Ray3(Vec3 Origin, Vec3 Direction)
{
    public Vec3 PointAt(float t) => Origin + (Direction * t);
    
    public bool Intersect(Sphere sphere, out float t0, out float t1)
    {
        t0 = float.NaN;
        t1 = float.NaN;

        // Source: http://kylehalladay.com/blog/tutorial/math/2013/12/24/Ray-Sphere-Intersection.html
        Vec3 delta = sphere.Origin - Origin;
        float tCenter = delta.Dot(Direction);
        if (tCenter < 0)
            return false;
        
        float dSquared = (tCenter * tCenter) - delta.Dot(delta);
        float radiusSquared = sphere.Radius * sphere.Radius;
        if (dSquared > radiusSquared)
            return false;

        float t1Center = MathF.Sqrt(radiusSquared - dSquared);
        t0 = tCenter - t1Center;
        t1 = tCenter + t1Center;

        if (t0 > t1)
            (t0, t1) = (t1, t0);
        
        return true;
    }

    public bool Intersect(Triangle3 triangle, out float t)
    {
        t = float.NaN;
        
        // Source: http://james-ramsden.com/calculate-the-cross-product-c-code/
        (Vec3 p1, Vec3 p2, Vec3 p3) = triangle;
        Vec3 e1 = p2 - p1;
        Vec3 e2 = p3 - p1;

        Vec3 p = Direction.Cross(e2);
        float det = e1.Dot(p);
        if (det.ApproxZero())
            return false;

        float invDet = 1.0f / det;
        Vec3 s = Origin - p1;

        float u = s.Dot(p) * invDet;
        if (u < 0 || u > 1)
            return false;

        Vec3 q = s.Cross(e1);
        float v = Direction.Dot(q) * invDet;
        if (v < 0 || u + v > 1)
            return false;

        t = e2.Dot(q) * invDet;
        return true;
    }

    public bool Intersects(Box3 box, out float t)
    {
        // Source: https://github.com/erich666/GraphicsGems/blob/master/gems/RayBox.c
        // TODO
        
        t = float.NaN;
        return false;
    }
}
using GlmSharp;
using Helion.Geometry.Spheres;
using System;

namespace Helion.Geometry.New;

public readonly record struct Ray2d(Vec2d Pos, Vec2d Dir)
{
    public Seg2d Seg(double distance) => (Pos, Pos + (Dir * distance));
    public Vec2d FromTime(double t) => Pos + (Dir * t);

    // TODO: Box slab intersect:
    // https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-box-intersection
}

public readonly record struct Ray3d(Vec3d Pos, Vec3d Dir)
{
    public Seg3d Seg(double distance) => (Pos, Pos + (Dir * distance));
    public Vec3d FromTime(double t) => Pos + (Dir * t);

    public bool TryIntersect(in Triangle3d triangle, out Vec3d point)
    {
        if (TryIntersect(triangle, out double t))
        {
            point = FromTime(t);
            return true;
        }

        point = default;
        return false;
    }

    public bool TryIntersect(in Triangle3d triangle, out double t)
    {
        t = default;

        // Moller-Trumbore intersection.
        // Source: https://answers.unity.com/questions/861719/a-fast-triangle-triangle-intersection-algorithm-fo.html
        Vec3d e1 = triangle.Second - triangle.First;
        Vec3d e2 = triangle.Third - triangle.First;
        Vec3d p = Dir.Cross(e2);

        double det = e1.Dot(p);
        if (det.ApproxZero())
            return false;

        double invDet = 1.0f / det;
        Vec3d dist = Pos - triangle.First;
        double u = dist.Dot(p) * invDet;
        if (u < 0 || u > 1) 
            return false; 

        Vec3d q = dist.Cross(e1);
        double v = Dir.Dot(q) * invDet;
        if (v < 0 || u + v > 1)
            return false;

        t = e2.Dot(q) * invDet;
        return t > double.Epsilon;
    }

    // If it intersects, returns the closest of the two points.
    public bool TryIntersect(in SphereD sphere, out Vec3d point)
    {
        if (TryIntersect(sphere, out double t))
        {
            point = FromTime(t);
            return true;
        }

        point = default;
        return false;
    }

    // If it intersects, returns the closest of the two points.
    public bool TryIntersect(in SphereD sphere, out double t)
    {
        // Source: https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-sphere-intersection
        Vec3d L = sphere.Center - Pos;
        double tca = L.Dot(Dir);
        if (tca < 0)
        {
            t = default;
            return false;
        }

        double r2 = sphere.Radius * sphere.Radius;
        double d2 = L.Dot(L) - (tca * tca);
        if (d2 > r2)
        {
            t = default;
            return false;
        }

        double thc = Math.Sqrt(r2 - d2);
        double t0 = tca - thc;
        double t1 = tca + thc;

        if (t0 > t1)
        {
            double temp = t0;
            t0 = t1;
            t1 = temp;
        }

        if (t0 < 0)
        {
            t0 = t1;
            if (t0 < 0)
            {
                t = default;
                return false;
            }
        }

        t = t0;
        return true;
    }
}

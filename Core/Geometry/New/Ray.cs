using GlmSharp;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Helion.Geometry.New;

// Dir should be normalized.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Ray2(Vec2 Pos, Vec2 Dir)
{
    public Ray2(Vec2 pos, float angle) : this(pos, (MathF.Cos(angle), MathF.Sin(angle)))
    {
    }

    public Seg2 Seg(float distance) => (Pos, Pos + (Dir * distance));
    public Vec2 FromTime(float t) => Pos + (Dir * t);

    public bool TryIntersect(in Seg2 seg, out float t)
    {
        Seg2 rayAsSeg = new(Pos, Pos + Dir);
        return rayAsSeg.TryLineIntersect(seg, out t) && t >= 0;
    }

    public bool TryIntersect(in Box2 box, out float t)
    {
        Ray2Accel ray = new(Pos, Dir);
        return ray.Intersect(box, out t);
    }

    public bool TryIntersect(in Circle circle, out float t)
    {
        // TODO

        t = default;
        return false;
    }
}

// We want to pre-calculate things that we will reuse, like the inverse, and
// the bit classification to accelerate bounding box intersection.
// BoxIntersectType: Bit 0 is if x < 0, bit 1 is if y < 0
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Ray2Accel(Vec2 Pos, Vec2 Dir, Vec2 InvDir, int BoxIntersectType)
{
    public Ray2Accel(Vec2 pos, Vec2 dir) :
        this(pos, dir, dir.Inverse, Convert.ToInt32(pos.X < 0.0f) + (Convert.ToInt32(pos.Y < 0.0f) << 1))
    {
    }

    public bool Intersect(in Box2 box, out float t)
    {
        // Unfortunately C# doesn't currently make it possible to have very readable
        // code and maximal C++ levels of performance, even with unsafed fixed buffers.
        // This work-around is the best I can come up with right now for speed, which
        // emulates switching on the sign of the bounding box coordinates to select
        // whether min or max should be used.
        // Source: https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-box-intersection
        float tMinX = 0;
        float tMaxX = 0;
        float tMinY = 0;
        float tMaxY = 0;

        switch (BoxIntersectType)
        {
        case 0b00: // x >= 0 and y >= 0: sign[0] = 0, sign[1] = 0
            tMinX = (box.Min.X - Pos.X) * InvDir.X;
            tMaxX = (box.Max.X - Pos.X) * InvDir.X;
            tMinY = (box.Min.Y - Pos.Y) * InvDir.Y;
            tMaxY = (box.Max.Y - Pos.Y) * InvDir.Y;
            break;
        case 0b01: // x < 0 and y >= 0: sign[0] = 1, sign[1] = 0
            tMinX = (box.Max.X - Pos.X) * InvDir.X;
            tMaxX = (box.Min.X - Pos.X) * InvDir.X;
            tMinY = (box.Min.Y - Pos.Y) * InvDir.Y;
            tMaxY = (box.Max.Y - Pos.Y) * InvDir.Y;
            break;
        case 0b10: // x >= 0 and y < 0: sign[0] = 0, sign[1] = 1
            tMinX = (box.Min.X - Pos.X) * InvDir.X;
            tMaxX = (box.Max.X - Pos.X) * InvDir.X;
            tMinY = (box.Max.Y - Pos.Y) * InvDir.Y;
            tMaxY = (box.Min.Y - Pos.Y) * InvDir.Y;
            break;
        case 0b11: // x < 0 and y < 0: sign[0] = 1, sign[1] = 1
        default:
            tMinX = (box.Max.X - Pos.X) * InvDir.X;
            tMaxX = (box.Min.X - Pos.X) * InvDir.X;
            tMinY = (box.Max.Y - Pos.Y) * InvDir.Y;
            tMaxY = (box.Min.Y - Pos.Y) * InvDir.Y;
            break;
        }

        if (tMinX > tMaxY || tMinY > tMaxX)
        {
            t = default;
            return false;
        }

        t = tMinY > tMinX ? tMinY : tMinX;
        return true; 
    }
}

// Dir should be normalized.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Ray3(Vec3 Pos, Vec3 Dir)
{
    public Seg3 Seg(float distance) => (Pos, Pos + (Dir * distance));
    public Vec3 FromTime(float t) => Pos + (Dir * t);

    public bool TryIntersect(in Box3 box, out Vec3 point)
    {
        if (TryIntersect(box, out float t))
        {
            point = FromTime(t);
            return true;
        }

        point = default;
        return false;
    }

    public bool TryIntersect(in Box3 box, out float t)
    {
        // TODO: Slab thing but for 3D

        t = default;
        return false;
    }

    private bool TryIntersectHelper(in Plane plane, out float t)
    {
        // Reminder that directions and normals must be normalized.
        float denominator = plane.Normal.Dot(Dir);
        if (denominator.ApproxZero())
        {
            t = default;
            return false;
        }

        t = -(plane.Normal.Dot(Pos) + plane.D) / denominator;
        return true;
    }

    public bool TryIntersect(in Plane plane, out Vec3 intersect)
    {
        if (TryIntersectHelper(plane, out float t) && t >= 0)
        {
            intersect = Pos + (Dir * t);
            return true;
        }

        intersect = default;
        return false;
    }

    public bool TryIntersect(in Triangle3 triangle, out Vec3 point)
    {
        if (TryIntersect(triangle, out float t))
        {
            point = FromTime(t);
            return true;
        }

        point = default;
        return false;
    }

    public bool TryIntersect(in Triangle3 triangle, out float t)
    {
        t = default;

        // Moller-Trumbore intersection.
        // Source: https://answers.unity.com/questions/861719/a-fast-triangle-triangle-intersection-algorithm-fo.html
        Vec3 e1 = triangle.Second - triangle.First;
        Vec3 e2 = triangle.Third - triangle.First;
        Vec3 p = Dir.Cross(e2);

        float det = e1.Dot(p);
        if (det.ApproxZero())
            return false;

        float invDet = 1.0f / det;
        Vec3 dist = Pos - triangle.First;
        float u = dist.Dot(p) * invDet;
        if (u < 0 || u > 1) 
            return false; 

        Vec3 q = dist.Cross(e1);
        float v = Dir.Dot(q) * invDet;
        if (v < 0 || u + v > 1)
            return false;

        t = e2.Dot(q) * invDet;
        return t > float.Epsilon;
    }

    // If it intersects, returns the closest of the two points.
    public bool TryIntersect(in Sphere sphere, out Vec3 point)
    {
        if (TryIntersect(sphere, out float t))
        {
            point = FromTime(t);
            return true;
        }

        point = default;
        return false;
    }

    // If it intersects, returns the closest of the two points.
    public bool TryIntersect(in Sphere sphere, out float t)
    {
        // Source: https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-sphere-intersection
        Vec3 L = sphere.Center - Pos;
        float tca = L.Dot(Dir);
        if (tca < 0)
        {
            t = default;
            return false;
        }

        float r2 = sphere.Radius * sphere.Radius;
        float d2 = L.Dot(L) - (tca * tca);
        if (d2 > r2)
        {
            t = default;
            return false;
        }

        float thc = MathF.Sqrt(r2 - d2);
        float t0 = tca - thc;
        float t1 = tca + thc;

        if (t0 > t1)
        {
            float temp = t0;
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

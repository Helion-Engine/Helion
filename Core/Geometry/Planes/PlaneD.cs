using Helion.Geometry.Vectors;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Planes;

public struct PlaneD
{
    public double A;
    public double B;
    public double C;
    public double D;
    private readonly double m_inverseC;

    public PlaneD(double a, double b, double c, double d)
    {
        Precondition(!MathHelper.IsZero(c), "A plane cannot have a zero Z coefficient");

        A = a;
        B = b;
        C = c;
        D = d;
        m_inverseC = 1.0 / c;
    }

    public void MoveZ(double amount)
    {
        D -= amount * C;
    }

    public double ToZ(Vec2D point)
    {
        return -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
    }

    public double ToZ(Vec3D point)
    {
        return -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
    }

    public readonly bool Intersects(in Vec3D p, in Vec3D q, ref Vec3D intersect)
    {
        // Unroll delta = q - p
        double dx = q.X - p.X;
        double dy = q.Y - p.Y;
        double dz = q.Z - p.Z;

        // Unroll denominator = normal.Dot(delta)
        var denominator = (A * dx) + (B * dy) + (C * dz);
        if (MathHelper.IsZero(denominator))
            return false;

        // Unroll t = -(normal.Dot(p) + D) / denominator
        var t = -((A * p.X) + (B * p.Y) + (C * p.Z) + D) / denominator;
        if (t < 0.0 || t > 1.0)
            return false;

        intersect.X = p.X + t * dx;
        intersect.Y = p.Y + t * dy;
        intersect.Z = p.Z + t * dz;

        return true;
    }

    public readonly bool IntersectsOld(in Vec3D p, in Vec3D q, ref Vec3D intersect)
    {
        Vec3D normal = (A, B, C);
        Vec3D delta = q - p;

        double denominator = normal.Dot(delta);
        if (MathHelper.IsZero(denominator))
            return false;

        double t = -(normal.Dot(p) + D) / denominator;
        if (t < 0.0 || t > 1.0)
            return false;

        intersect = p + (t * delta);
        return true;
    }
}

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

    public bool Intersects(in Vec3D p, in Vec3D q, ref Vec3D intersect)
    {
        Vec3D normal = (A, B, C);
        Vec3D delta = q - p;

        double denominator = normal.Dot(delta);
        if (MathHelper.IsZero(denominator))
            return false;

        double t = -(normal.Dot(p) + D) / denominator;
        intersect = p + (t * delta);
        return true;
    }
}

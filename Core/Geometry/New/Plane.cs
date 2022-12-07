namespace Helion.Geometry.New;

public readonly struct PlaneD
{
    public readonly double A;
    public readonly double B;
    public readonly double C;
    public readonly double D;
    private readonly double m_inverseC;

    public Vec3d Normal => (A, B, C);

    private PlaneD(Vec3d normalUnit, double d, double inverseC)
    {
        A = normalUnit.X;
        B = normalUnit.Y;
        C = normalUnit.Z;
        D = d;
        m_inverseC = inverseC;
    }

    public PlaneD(double a, double b, double c, double d) : this(new Vec3d(a, b, c).Unit, d, 1.0 / c)
    {
    }

    public PlaneD MoveZ(double amount) => new(Normal, D - (amount * C), m_inverseC);
    public double Z(Vec2d point) => -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
    public double Z(Vec3d point) => -(D + (A * point.X) + (B * point.Y)) * m_inverseC;

    //=========================================================================
    // TODO: Move to segment and/or ray class
    private bool TryIntersectHelper(in Vec3d pos, in Vec3d dir, out double t)
    {
        double denominator = Normal.Dot(dir);
        if (denominator.ApproxZero())
        {
            t = default;
            return false;
        }

        t = -(Normal.Dot(pos) + D) / denominator;
        return true;
    }

    public bool TryIntersect(in Seg3d seg, out Vec3d intersect)
    {
        Vec3d delta = seg.Delta;
        if (TryIntersectHelper(seg.Start, delta, out double t) && t >= 0 && t <= 1)
        {
            intersect = seg.Start + (delta * t);
            return true;
        }

        intersect = default;
        return false;
    }

    public bool TryIntersect(in Ray3d ray, out Vec3d intersect)
    {
        if (TryIntersectHelper(ray.Pos, ray.Dir, out double t) && t >= 0)
        {
            intersect = ray.Pos + (ray.Dir * t);
            return true;
        }

        intersect = default;
        return false;
    }
    //=========================================================================
}

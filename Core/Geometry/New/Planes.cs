namespace Helion.Geometry.New;

public readonly struct PlaneD
{
    public readonly double A;
    public readonly double B;
    public readonly double C;
    public readonly double D;
    private readonly double m_inverseC;

    public Vec3d Normal => (A, B, C);

    private PlaneD(double a, double b, double c, double d, double inverseC)
    {
        A = a;
        B = b;
        C = c;
        D = d;
        m_inverseC = inverseC;
    }

    public PlaneD(double a, double b, double c, double d) : this(a, b, c, d, 1.0 / c)
    {
    }

    public PlaneD MoveZ(double amount) => new(A, B, C, D - (amount * C), m_inverseC);
    public double Z(Vec2d point) => -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
    public double Z(Vec3d point) => -(D + (A * point.X) + (B * point.Y)) * m_inverseC;

    public bool TryIntersect(in Seg3d seg, out Vec3d intersect)
    {
        Vec3d delta = seg.Delta;
        double denominator = Normal.Dot(delta);
        if (denominator.ApproxZero())
        {
            intersect = default;
            return false;
        }

        double t = -(Normal.Dot(seg.Start) + D) / denominator;
        intersect = seg.Start + (t * delta);
        return t >= 0 && t <= 1;
    }

    public bool TryIntersect(in Ray3d ray, out Vec3d intersect)
    {
        double denominator = Normal.Dot(ray.Dir);
        if (denominator.ApproxZero())
        {
            intersect = default;
            return false;
        }

        double t = -(Normal.Dot(ray.Pos) + D) / denominator;
        intersect = ray.Pos + (t * ray.Dir);
        return t >= 0;
    }
}

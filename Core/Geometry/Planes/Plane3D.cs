using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Planes
{
    /// <summary>
    /// A three dimensional plane (that is not allowed to be vertical along the
    /// Z axis) in the form of Ax + By + Cz + D = 0.
    /// </summary>
    public readonly struct Plane3D
    {
        public readonly double A;
        public readonly double B;
        public readonly double C;
        public readonly double D;
        private readonly double m_inverseC;

        public Plane3D(double a, double b, double c, double d)
        {
            Precondition(!c.ApproxZero(), "A plane cannot have a zero Z coefficient");

            A = a;
            B = b;
            C = c;
            D = d;
            m_inverseC = 1.0 / c;
        }
        
        private Plane3D(double a, double b, double c, double d, double inverseC)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            m_inverseC = inverseC;
        }

        public Plane3D MoveAlongZ(double amount)
        {
            return new Plane3D(A, B, C, D - amount * C, m_inverseC);
        }

        public double ToZ(Vec2D point)
        {
            return -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
        }

        public double ToZ(Vec3D point)
        {
            return -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
        }

        public bool Intersects(Vec3D p1, Vec3D p2, ref Vec3D intersect)
        {
            double top = -(D + (A * p1.X) + (B * p1.Y) + (C * p1.Z));
            double bottom = (A * p2.X) + (B * p2.X) + (C * p2.Z);

            if (bottom.ApproxZero())
                return false;

            double t = top / bottom;
            intersect = p1.Interpolate(p2, t);

            return true;
        }
    }
}

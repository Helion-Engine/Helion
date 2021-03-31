using Helion.Geometry.Vectors;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Planes
{
    public class PlaneD
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

        public bool Intersects(in Vec3D p1, in Vec3D p2, ref Vec3D intersect)
        {
            double top = -(D + (A * p1.X) + (B * p1.Y) + (C * p1.Z));
            double bottom = (A * p2.X) + (B * p2.X) + (C * p2.Z);

            if (MathHelper.IsZero(bottom))
                return false;

            double t = top / bottom;
            intersect = p1 + ((p2 - p1) * t);

            return true;
        }
    }
}
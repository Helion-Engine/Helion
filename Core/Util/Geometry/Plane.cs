using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Geometry
{
    public class PlaneD
    {
        public double A;
        public double B;
        public double C;
        public double D;
        private readonly double m_inverseC;

        public PlaneD(double height) : this(0, 0, 1, -height)
        {
        }

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
    }
}
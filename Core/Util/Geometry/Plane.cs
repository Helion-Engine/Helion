using System.Numerics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Geometry
{
    public class PlaneD
    {
        public double A;
        public double B;
        public double C;
        public double D;
        public bool IsFlat;
        public double FlatHeight;
        private readonly double m_inverseC;

        public PlaneD(double height) : this(0, 0, 1, -height)
        {
        }

        public PlaneD(double a, double b, double c, double d)
        {
            Precondition(!(MathHelper.IsZero(a) && MathHelper.IsZero(b) && MathHelper.IsZero(c)), "Plane has all zero a/b/c coefficients");

            A = a;
            B = b;
            C = c;
            D = d;
            IsFlat = MathHelper.IsZero(a) && MathHelper.IsZero(b);

            if (IsFlat)
            {
                D = d / c;
                C = 1.0;
                FlatHeight = -d;
            }
            else
                m_inverseC = 1.0 / c;
        }

        public void MoveZ(double amount)
        {
            D -= amount * C;
        }
        
        public double ToZ(Vec2D point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
        }

        public double ToZ(Vec3D point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
        }
    }

    public class PlaneF
    {
        public float A;
        public float B;
        public float C;
        public float D;
        public bool IsFlat;
        public float FlatHeight;
        private readonly float m_inverseC;

        public PlaneF(float height) : this(0, 0, 1, -height)
        {
        }

        public PlaneF(float a, float b, float c, float d)
        {
           Precondition(!(MathHelper.IsZero(a) && MathHelper.IsZero(b) && MathHelper.IsZero(c)), "Plane has all zero a/b/c coefficients");

            A = a;
            B = b;
            C = c;
            D = d;
            IsFlat = MathHelper.IsZero(a) && MathHelper.IsZero(b);

            if (IsFlat)
            {
                D = d / c;
                C = 1.0f;
                FlatHeight = -d;
            }
            else
                m_inverseC = 1.0f / c;
        }

        public void MoveZ(float amount)
        {
            D -= amount * C;
        }
        
        public double ToZ(Vector2 point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
        }

        public double ToZ(Vector3 point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
        }
    }

    public class PlaneFixed
    {
        public Fixed A;
        public Fixed B;
        public Fixed C;
        public Fixed D;
        public bool IsFlat;
        public Fixed FlatHeight;
        private readonly Fixed m_inverseC;

        public PlaneFixed(Fixed height) : this(Fixed.Zero(), Fixed.Zero(), Fixed.One(), -height)
        {
        }

        public PlaneFixed(Fixed a, Fixed b, Fixed c, Fixed d)
        {
            Precondition(!(MathHelper.IsZero(a) && MathHelper.IsZero(b) && MathHelper.IsZero(c)), "Plane has all zero a/b/c coefficients");

            A = a;
            B = b;
            C = c;
            D = d;
            IsFlat = MathHelper.IsZero(a) && MathHelper.IsZero(b);

            if (IsFlat)
            {
                D = d / c;
                C = Fixed.One();
                FlatHeight = -d;
            }
            else
                m_inverseC = Fixed.One() / c;
        }
        
        public void MoveZ(Fixed amount)
        {
            D -= amount * C;
        }

        public Fixed ToZ(Vec2Fixed point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
        }

        public Fixed ToZ(Vec3Fixed point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
        }
    }
}
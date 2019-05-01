using System.Numerics;

namespace Helion.Util.Geometry
{
    public class PlaneD
    {
        public readonly double A;
        public readonly double B;
        public readonly double C;
        public readonly double D;
        public readonly bool IsFlat;
        public readonly double FlatHeight;
        private readonly double inverseC;

        public PlaneD(double height) : this(0, 0, 1, -height)
        {
        }

        public PlaneD(double a, double b, double c, double d)
        {
            Assert.Precondition(!(MathHelper.IsZero(a) && MathHelper.IsZero(b) && MathHelper.IsZero(c)), "Plane has all zero a/b/c coefficients");

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
                inverseC = 1.0 / c;
        }

        public double ToZ(Vec2D point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * inverseC;
        }

        public double ToZ(Vec3D point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * inverseC;
        }
    }

    public class PlaneF
    {
        public readonly float A;
        public readonly float B;
        public readonly float C;
        public readonly float D;
        public readonly bool IsFlat;
        public readonly float FlatHeight;
        private readonly float inverseC;

        public PlaneF(float height) : this(0, 0, 1, -height)
        {
        }

        public PlaneF(float a, float b, float c, float d)
        {
            Assert.Precondition(!(MathHelper.IsZero(a) && MathHelper.IsZero(b) && MathHelper.IsZero(c)), "Plane has all zero a/b/c coefficients");

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
                inverseC = 1.0f / c;
        }

        public double ToZ(Vector2 point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * inverseC;
        }

        public double ToZ(Vector3 point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * inverseC;
        }
    }

    public class PlaneFixed
    {
        public readonly Fixed A;
        public readonly Fixed B;
        public readonly Fixed C;
        public readonly Fixed D;
        public readonly bool IsFlat;
        public readonly Fixed FlatHeight;
        private readonly Fixed inverseC;

        public PlaneFixed(Fixed height) :
            this(Fixed.Zero, Fixed.Zero, Fixed.One, Fixed.FromInt(-height))
        {
        }

        public PlaneFixed(Fixed a, Fixed b, Fixed c, Fixed d)
        {
            Assert.Precondition(!(MathHelper.IsZero(a) && MathHelper.IsZero(b) && MathHelper.IsZero(c)), "Plane has all zero a/b/c coefficients");

            A = a;
            B = b;
            C = c;
            D = d;
            IsFlat = MathHelper.IsZero(a) && MathHelper.IsZero(b);

            if (IsFlat)
            {
                D = d / c;
                C = Fixed.One;
                FlatHeight = -d;
            }
            else
                inverseC = Fixed.One / c;
        }

        public double ToZ(Vec2Fixed point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * inverseC;
        }

        public double ToZ(Vec3Fixed point)
        {
            return IsFlat ? -D : -(D + (A * point.X) + (B * point.Y)) * inverseC;
        }
    }
}

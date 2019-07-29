using System;

namespace Helion.Util.Geometry
{
    /// <summary>
    /// An implementation of a 16.16 fixed point number as a 32-bit integer.
    /// </summary>
    public struct Fixed
    {
        /// <summary>
        /// How many bits make up a single fractional unit.
        /// </summary>
        public const int UnitBits = 16;

        /// <summary>
        /// The mask for the lower fractional bits.
        /// </summary>
        public const int FractionalMask = 0x0000FFFF;

        /// <summary>
        /// The mask for the upper integral bits.
        /// </summary>
        public const int IntegralMask = FractionalMask << UnitBits;

        /// <summary>
        /// A small epsilon value that can be used in comparisons.
        /// </summary>
        public static Fixed Epsilon() => new Fixed(0x00000008);

        /// <summary>
        /// A representation of 0.0.
        /// </summary>
        public static Fixed Zero() => FromInt(0);

        /// <summary>
        /// A representation of 1.0.
        /// </summary>
        public static Fixed One() => FromInt(1);

        /// <summary>
        /// A representation of -1.0.
        /// </summary>
        public static Fixed NegativeOne() => FromInt(-1);

        /// <summary>
        /// The most negative fixed point value.
        /// </summary>
        public static Fixed Lowest() => new Fixed(0x80000000);

        /// <summary>
        /// The largest fixed point value.
        /// </summary>
        public static Fixed Max() => new Fixed(0x7FFFFFFF);

        /// <summary>
        /// The bits that make up the fixed point number.
        /// </summary>
        public readonly int Bits;

        /// <summary>
        /// Creates a fixed point number from the bits provided.
        /// </summary>
        /// <param name="bits">The bits to make up the number.</param>
        public Fixed(int bits) => Bits = bits;

        /// <summary>
        /// Creates a fixed point value from the number provided.
        /// </summary>
        /// <param name="f">The floating point value to convert.</param>
        public Fixed(float f) : this((int)(f * 65536.0f)) { }

        /// <summary>
        /// Creates a fixed point value from the number provided.
        /// </summary>
        /// <param name="d">The double to convert.</param>
        public Fixed(double d) : this((int)(d * 65536.0)) { }

        /// <summary>
        /// Creates a fixed point value from and upper 16 and lower 16 bit set 
        /// of value.
        /// </summary>
        /// <param name="upper">The upper 16 bits.</param>
        /// <param name="lower">The lower 16 bits.</param>
        public Fixed(short upper, ushort lower) : this(BitsFromUpperAndLower(upper, lower)) { }

        /// <summary>
        /// Takes an integer and turns it into fixed point. This means a value
        /// of 24 would become 24.0 in fixed point, not 24/65536.
        /// </summary>
        /// <param name="i">The integer to make into fixed point.</param>
        /// <returns>The fixed point value for the integer.</returns>
        public static Fixed FromInt(int i) => new Fixed(i << UnitBits);

        private static int BitsFromUpperAndLower(short upper, ushort lower)
        {
            uint bits = (uint)((ushort)upper << UnitBits);
            return (int)(bits | lower);
        }

        public static Fixed operator -(Fixed value) => new Fixed(-value.Bits);
        public static Fixed operator +(Fixed self, Fixed other) => new Fixed(self.Bits + other.Bits);
        public static Fixed operator +(Fixed self, int value) => new Fixed(self.Bits + (value << UnitBits));
        public static Fixed operator -(Fixed self, Fixed other) => new Fixed(self.Bits - other.Bits);
        public static Fixed operator -(Fixed self, int value) => new Fixed(self.Bits - (value << UnitBits));
        public static Fixed operator *(Fixed self, Fixed other) => new Fixed(((ulong)self.Bits * (ulong)other.Bits) >> UnitBits);
        public static Fixed operator *(Fixed self, int value) => new Fixed(((ulong)self.Bits * (ulong)value) >> UnitBits);
        public static Fixed operator *(int value, Fixed self) => new Fixed(((ulong)self.Bits * (ulong)value) >> UnitBits);
        public static Fixed operator /(Fixed numerator, int value) => new Fixed(numerator.Bits / value);
        public static Fixed operator <<(Fixed self, int bits) => new Fixed(self.Bits << bits);
        public static Fixed operator >>(Fixed self, int bits) => new Fixed(self.Bits >> bits);
        public static Fixed operator &(Fixed numerator, int bits) => new Fixed(numerator.Bits & bits);
        public static Fixed operator |(Fixed numerator, int bits) => new Fixed(numerator.Bits | bits);
        public static bool operator ==(Fixed self, Fixed other) => self.Bits == other.Bits;
        public static bool operator !=(Fixed self, Fixed other) => !(self == other);
        public static bool operator >(Fixed self, Fixed value) => self.Bits > value.Bits;
        public static bool operator >=(Fixed self, Fixed value) => self.Bits >= value.Bits;
        public static bool operator <(Fixed self, Fixed value) => self.Bits < value.Bits;
        public static bool operator <=(Fixed self, Fixed value) => self.Bits <= value.Bits;
        public static bool operator >(Fixed self, int value) => self.Bits > (value << UnitBits);
        public static bool operator >=(Fixed self, int value) => self.Bits >= (value << UnitBits);
        public static bool operator <(Fixed self, int value) => self.Bits < (value << UnitBits);
        public static bool operator <=(Fixed self, int value) => self.Bits <= (value << UnitBits);

        public static Fixed operator /(Fixed numerator, Fixed denominator)
        {
            // This is not an optimization anymore, but it prevents numbers
            // that are really far apart from overflowing or becoming zero.
            if ((Math.Abs(numerator.Bits) >> 14) >= Math.Abs(denominator.Bits))
                return new Fixed((numerator.Bits ^ denominator.Bits) < 0 ? 0x80000000 : 0x7FFFFFFF);
            else
                return new Fixed((((ulong)numerator.Bits) << UnitBits) / (ulong)denominator.Bits);
        }

        public Fixed Floor()
        {
            if (Bits < 0)
                return new Fixed(((Bits - FractionalMask) & IntegralMask) + 1); 
            else
                return new Fixed(Bits & IntegralMask);
        }

        public Fixed Abs() => new Fixed(Math.Abs(Bits));
        public Fixed Sqrt() => new Fixed(Math.Sqrt(ToDouble()));
        public Fixed Inverse() => One() / this;
        public int ToInt() => Bits >> UnitBits;
        public float ToFloat() => Bits / 65536.0f;
        public double ToDouble() => Bits / 65536.0;

        public override string ToString() => $"{(float)Bits}";
        public override bool Equals(object obj) => obj is Fixed f && Bits == f.Bits;
        public override int GetHashCode() => Bits.GetHashCode();
    }
}

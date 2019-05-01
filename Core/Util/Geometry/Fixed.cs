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
        public static readonly int UnitBits = 16;

        /// <summary>
        /// A small epsilon value that can be used in comparisons.
        /// </summary>
        public static readonly Fixed Epsilon = new Fixed(0x00000008);

        /// <summary>
        /// A representation of 0.0.
        /// </summary>
        public static readonly Fixed Zero = FromInt(0);

        /// <summary>
        /// A representation of 1.0.
        /// </summary>
        public static readonly Fixed One = FromInt(1);

        /// <summary>
        /// A representation of -1.0.
        /// </summary>
        public static readonly Fixed NegativeOne = FromInt(-1);

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
        public Fixed(ushort upper, ushort lower) : this((upper << UnitBits) | lower) { }

        /// <summary>
        /// Takes an integer and turns it into fixed point. This means a value
        /// of 24 would become 24.0 in fixed point, not 24/65536.
        /// </summary>
        /// <param name="i">The integer to make into fixed point.</param>
        /// <returns>The fixed point value for the integer.</returns>
        public static Fixed FromInt(int i) => new Fixed(i << UnitBits);

        public static implicit operator int(Fixed f) => f.Bits >> UnitBits;
        public static implicit operator float(Fixed f) => f.Bits / 65536.0f;
        public static implicit operator double(Fixed f) => f.Bits / 65536.0;

        public static Fixed operator -(Fixed value) => new Fixed(-value.Bits);
        public static Fixed operator +(Fixed self, Fixed other) => new Fixed(self.Bits + other.Bits);
        public static Fixed operator -(Fixed self, Fixed other) => new Fixed(self.Bits - other.Bits);
        public static Fixed operator *(Fixed self, Fixed other) => new Fixed(((ulong)self.Bits * (ulong)other.Bits) >> UnitBits);
        public static Fixed operator *(Fixed self, int value) => new Fixed(((ulong)self.Bits * (ulong)value) >> UnitBits);
        public static Fixed operator *(int value, Fixed self) => new Fixed(((ulong)self.Bits * (ulong)value) >> UnitBits);
        public static Fixed operator /(Fixed numerator, int value) => new Fixed(numerator.Bits / value);
        public static Fixed operator /(Fixed numerator, Fixed denominator)
        {
            // This is not an optimization anymore, but it prevents numbers
            // that are really far apart from overflowing or becoming zero.
            if ((Math.Abs(numerator.Bits) >> 14) >= Math.Abs(denominator.Bits))
                return new Fixed((numerator.Bits ^ denominator.Bits) < 0 ? 0x80000000 : 0x7FFFFFFF);
            else
                return new Fixed((((ulong)numerator.Bits) << UnitBits) / (ulong)denominator.Bits);
        }

        public static Fixed operator <<(Fixed self, int bits) => new Fixed(self.Bits << bits);
        public static Fixed operator >>(Fixed self, int bits) => new Fixed(self.Bits >> bits);
        public static bool operator ==(Fixed self, Fixed other) => self.Bits == other.Bits;
        public static bool operator !=(Fixed self, Fixed other) => !(self == other);

        public Fixed Inverse() => One / this;

        public override string ToString() => $"{(float)Bits}";
        public override bool Equals(object obj) => obj is Fixed f && Bits == f.Bits;
        public override int GetHashCode() => HashCode.Combine(Bits);
    }
}

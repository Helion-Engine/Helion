using System;

namespace Helion.Util.Geometry
{
    /// <summary>
    /// Represents a deterministic angle in bit form.
    /// </summary>
    /// <remarks>
    /// All bits are used in determining the angle.
    /// </remarks>
    public struct Angle
    {
        public static readonly Angle East = new Angle(0);
        public static readonly Angle NorthEast = new Angle(0x2000000U);
        public static readonly Angle North = new Angle(0x4000000U);
        public static readonly Angle NorthWest = new Angle(0x6000000U);
        public static readonly Angle West = new Angle(0x80000000U);
        public static readonly Angle SouthWest = new Angle(0xA0000000U);
        public static readonly Angle South = new Angle(0xC0000000U);
        public static readonly Angle SouthEast = new Angle(0xE0000000U);

        /// <summary>
        /// The bits that make up the angle.
        /// </summary>
        public readonly uint Bits;

        public Angle(uint bits)
        {
            Bits = bits;
        }

        public Angle(float radians) : this(BitsFromRadians(radians))
        {
        }

        public Angle(double radians) : this(BitsFromRadians(radians))
        {
        }

        private static uint BitsFromRadians(double radians) 
        {
            double unit = radians / (2 * Math.PI);
            return (uint)(unit * uint.MaxValue);
        }

        public static Angle operator +(Angle self, Angle other) => new Angle(self.Bits + other.Bits);
        public static Angle operator -(Angle self, Angle other) => new Angle(self.Bits - other.Bits);
        public static Angle operator *(Angle self, int value) => new Angle(self.Bits * value);
        public static Angle operator /(Angle self, int value) => new Angle(self.Bits / value);
        public static Angle operator >>(Angle self, int value) => new Angle(self.Bits >> value);
        public static Angle operator <<(Angle self, int value) => new Angle(self.Bits << value);
        public static Angle operator &(Angle self, uint value) => new Angle(self.Bits & value);
        public static Angle operator |(Angle self, uint value) => new Angle(self.Bits | value);

        // TODO: Implement a deterministic lookup table.

        public double Sin() => Math.Sin(ToRadians());
        public double Cos() => Math.Cos(ToRadians());
        public double ToRadians() => Bits / uint.MaxValue * 2 * Math.PI;
        public double ToDegrees() => Bits / uint.MaxValue * 360;
        public override string ToString() => $"{ToRadians()}";
    }
}

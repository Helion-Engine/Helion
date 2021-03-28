using System;

namespace Helion.Geometry
{
    /// <summary>
    /// Represents a deterministic angle in bit form.
    /// </summary>
    /// <remarks>
    /// All bits are used in determining the angle.
    /// </remarks>
    public struct Angle
    {
        public static readonly Angle East = new(0);
        public static readonly Angle NorthEast = new(0x2000000U);
        public static readonly Angle North = new(0x4000000U);
        public static readonly Angle NorthWest = new(0x6000000U);
        public static readonly Angle West = new(0x80000000U);
        public static readonly Angle SouthWest = new(0xA0000000U);
        public static readonly Angle South = new(0xC0000000U);
        public static readonly Angle SouthEast = new(0xE0000000U);

        /// <summary>
        /// The bits that make up the angle.
        /// </summary>
        public readonly uint Bits;
        
        public double Radians => Bits / uint.MaxValue * 2 * Math.PI;
        public double Degrees => Bits / uint.MaxValue * 360;

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

        public static Angle operator +(Angle self, Angle other) => new(self.Bits + other.Bits);
        public static Angle operator -(Angle self, Angle other) => new(self.Bits - other.Bits);
        public static Angle operator *(Angle self, int value) => new(self.Bits * value);
        public static Angle operator /(Angle self, int value) => new(self.Bits / value);
        public static Angle operator >>(Angle self, int value) => new(self.Bits >> value);
        public static Angle operator <<(Angle self, int value) => new(self.Bits << value);
        public static Angle operator &(Angle self, uint value) => new(self.Bits & value);
        public static Angle operator |(Angle self, uint value) => new(self.Bits | value);

        // TODO: Implement a deterministic lookup table.
        
        public double Sin() => Math.Sin(Radians);
        public double Cos() => Math.Cos(Radians);
        
        public override string ToString() => Radians.ToString();
    }
}

using System;

namespace Helion.GeometryNew;

/// <summary>
/// An implementation of a 16.16 fixed point number as a 32-bit integer.
/// </summary>
public readonly struct Fixed
{
    public const int UnitBits = 16;
    public const int FractionalMask = 0x0000FFFF;
    public const int IntegralMask = FractionalMask << UnitBits;

    public static Fixed Epsilon() => new(0x00000008);
    public static Fixed Zero() => From(0);
    public static Fixed One() => From(1);
    public static Fixed NegativeOne() => From(-1);
    public static Fixed Lowest() => new Fixed(0x80000000);
    public static Fixed Max() => new Fixed(0x7FFFFFFF);
    
    public readonly int Bits;

    private Fixed(int bits)
    {
        Bits = bits;   
    }

    private Fixed(float f) : this((int)(f * 65536.0f))
    {
    }

    private Fixed(double d) : this((int)(d * 65536.0))
    {
    }
    
    public Fixed(short upper, ushort lower) : this(BitsFromUpperAndLower(upper, lower))
    {
    }

    public static Fixed FromBits(int bits) => new(bits);
    public static Fixed From(int i) => new(i << UnitBits);
    public static Fixed From(float f) => new(f);
    public static Fixed From(double d) => new(d);

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
        return new Fixed((((ulong)numerator.Bits) << UnitBits) / (ulong)denominator.Bits);
    }

    public Fixed Floor()
    {
        if (Bits < 0)
            return new Fixed(((Bits - FractionalMask) & IntegralMask) + 1);
        return new Fixed(Bits & IntegralMask);
    }

    public Fixed Min(Fixed other) => Bits < other.Bits ? this : other;
    public Fixed Max(Fixed other) => Bits > other.Bits ? this : other;
    public Fixed Abs() => new(Math.Abs(Bits));
    public Fixed Sqrt() => new(Math.Sqrt(ToDouble()));
    public Fixed Inverse() => One() / this;
    public int ToInt() => Bits >> UnitBits;
    public float ToFloat() => Bits / 65536.0f;
    public double ToDouble() => Bits / 65536.0;

    public override string ToString() => $"{ToDouble()}";
    public override bool Equals(object? obj) => obj is Fixed f && Bits == f.Bits;
    public override int GetHashCode() => Bits.GetHashCode();
}
using System;

namespace Helion.Geometry.New;

/// <summary>
/// An implementation of a 16.16 fixed point number as a 32-bit integer.
/// </summary>
public readonly struct Fixed
{
    public const int UnitBits = 16;
    public const int FractionalMask = 0x0000FFFF;
    public const int IntegralMask = FractionalMask << UnitBits;

    public readonly int Bits;

    public int Int => Bits >> UnitBits;
    public float Float => Bits / 65536.0f;
    public double Double => Bits / 65536.0;

    public Fixed(short upper, ushort lower)
    {
        uint bits = (uint)((ushort)upper << UnitBits);
        Bits = (int)(bits | lower);
    }

    public Fixed(int i)
    {
        Bits = i << UnitBits;
    }

    public Fixed(float f)
    {
        Bits = (int)(f * 65536.0f);
    }

    public Fixed(double d)
    {
        Bits = (int)(d * 65536.0);
    }

    public static Fixed FromBits(int bits) => new((short)(bits >>> 16), (ushort)(bits & 0x0000FFFF));

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

    public override string ToString() => $"{Double}";
    public override bool Equals(object? obj) => obj is Fixed f && Bits == f.Bits;
    public override int GetHashCode() => Bits.GetHashCode();
}

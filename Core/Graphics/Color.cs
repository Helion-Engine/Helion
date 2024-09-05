using Helion.Geometry.Vectors;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Helion.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Color
{
    public static readonly Color Transparent = (0, 0, 0, 0);

    public static readonly Color Black = (0x00, 0x00, 0x00);
    public static readonly Color Blue = (0x00, 0x00, 0xFF);
    public static readonly Color Brown = (0xA5, 0x2A, 0x2A);
    public static readonly Color Chocolate = (0xD2, 0x69, 0x1E);
    public static readonly Color Cyan = (0x00, 0xFF, 0xFF);
    public static readonly Color DarkBrown = (0x64, 0x16, 0x16);
    public static readonly Color DarkGray = (0x49, 0x49, 0x49);
    public static readonly Color DarkGreen = (0x00, 0x64, 0x00);
    public static readonly Color DarkRed = (0x8B, 0x00, 0x00);
    public static readonly Color Firebrick = (0xB2, 0x22, 0x22);
    public static readonly Color Gold = (0xFF, 0xD7, 0x00);
    public static readonly Color Gray = (0x80, 0x80, 0x80);
    public static readonly Color Green = (0x00, 0xFF, 0x00);
    public static readonly Color Khaki = (0xF0, 0xE6, 0x8C);
    public static readonly Color LawnGreen = (0x7C, 0xFC, 0x00);
    public static readonly Color LightBlue = (0xAD, 0xD8, 0xE6);
    public static readonly Color LightGreen = (0x90, 0xEE, 0x90);
    public static readonly Color Olive = (0x80, 0x80, 0x00);
    public static readonly Color Orange = (0xFF, 0xA5, 0x00);
    public static readonly Color PeachPuff = (0xFF, 0xDA, 0xB9);
    public static readonly Color Purple = (0x80, 0x00, 0x80);
    public static readonly Color Red = (0xFF, 0x00, 0x00);
    public static readonly Color RosyBrown = (0xBC, 0x8F, 0x8F);
    public static readonly Color SaddleBrown = (0x8B, 0x45, 0x13);
    public static readonly Color Tan = (0xD2, 0xB4, 0x8C);
    public static readonly Color White = (0xFF, 0xFF, 0xFF);
    public static readonly Color Yellow = (0xFF, 0xFF, 0x00);

    public uint m_value;

    public byte A => (byte)((m_value & 0xFF000000) >> 24);
    public byte R => (byte)((m_value & 0x00FF0000) >> 16);
    public byte G => (byte)((m_value & 0x0000FF00) >> 8);
    public byte B => (byte)(m_value & 0x000000FF);

    public Color(byte a, byte r, byte g, byte b)
    {
        m_value = FromBytes(a, r, g, b);
    }

    public static uint FromBytes(byte a, byte r, byte g, byte b) => (uint)((a << 24) | (r << 16) | (g << 8) | b);

    public uint Uint => m_value;
    public Vec4F Normalized => new(A / 255.0f, R / 255.0f, G / 255.0f, B / 255.0f);
    public Vec3F Normalized3 => new(R / 255.0f, G / 255.0f, B / 255.0f);
    public SixLabors.ImageSharp.Color ToImageSharp => new(new Rgba32(R, G, B, A));

    public Color(Vec4F normalized) :
        this((byte)(normalized.X * 255), (byte)(normalized.Y * 255), (byte)(normalized.Z * 255), (byte)(normalized.W * 255))
    {
    }

    public Color(Vec3I bytes) :
        this(255, (byte)bytes.X, (byte)bytes.Y, (byte)bytes.Z)
    {

    }

    public Color(byte R, byte G, byte B) : this(255, R, G, B)
    {
    }

    public Color(uint argb)
    {
        m_value = argb;
    }

    public static Color Indexed(byte index) => new(index, 0, 0);

    public static implicit operator Color(ValueTuple<byte, byte, byte> tuple)
    {
        return new(tuple.Item1, tuple.Item2, tuple.Item3);
    }

    public static implicit operator Color(ValueTuple<byte, byte, byte, byte> tuple)
    {
        return new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    }

    public void Deconstruct(out byte a, out byte r, out byte g, out byte b)
    {
        a = A;
        r = R;
        g = G;
        b = B;
    }

    public static Color FromInts(int a, int r, int g, int b)
    {
        return new((byte)Math.Clamp(a, 0, 255), (byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255));
    }

    public static Color FromHSV(int hue, int saturation, int value)
    {
        hue = Math.Clamp(hue, 0, 359);
        saturation = Math.Clamp(saturation, 0, 100);
        value = Math.Clamp(value, 0, 100);

        int[] rgb = new int[3];

        int baseColor = (hue + 60) % 360 / 120;
        int shift = (hue + 60) % 360 - (120 * baseColor + 60);
        int secondaryColor = (baseColor + (shift >= 0 ? 1 : -1) + 3) % 3;

        // Hue
        rgb[baseColor] = 255;
        rgb[secondaryColor] = (int)(Math.Abs(shift) / 60.0f * 255.0f);

        // Saturation
        for (int i = 0; i < 3; i++)
            rgb[i] += (int)((255 - rgb[i]) * ((100 - saturation) / 100.0f));

        // Value
        for (int i = 0; i < 3; i++)
            rgb[i] -= (int)(rgb[i] * (100 - value) / 100.0f);

        return FromInts(255, rgb[0], rgb[1], rgb[2]);
    }

    // `t` is [0.0, 1.0], it does not saturate if out of range. Returns the
    // linearly interpolated color, where t = 0 is equal to this, and t = 1
    // is equal to `other`.
    public static Color Lerp(Vec4F normalized, Color other, float t)
    {
        Vec4F otherNormal = other.Normalized;
        Vec4F delta = otherNormal - normalized;
        return new(normalized + (delta * t));
    }

    public static Color FromName(string name)
    {
        return name.ToLower() switch
        {
            "black" => Black,
            "blue" => Blue,
            "brown" => Brown,
            "chocolate" => Chocolate,
            "cyan" => Cyan,
            "darkbrown" => DarkBrown,
            "darkgray" => DarkGray,
            "darkgrey" => DarkGray,
            "darkgreen" => DarkGreen,
            "darkred" => DarkRed,
            "firebrick" => Firebrick,
            "gold" => Gold,
            "gray" => Gray,
            "grey" => Gray,
            "green" => Green,
            "khaki" => Khaki,
            "lawngreen" => LawnGreen,
            "lightblue" => LightBlue,
            "lightgreen" => LightGreen,
            "olive" => Olive,
            "orange" => Orange,
            "peachpuff" => PeachPuff,
            "purple" => Purple,
            "red" => Red,
            "rosybrown" => RosyBrown,
            "saddlebrown" => SaddleBrown,
            "tan" => Tan,
            "white" => White,
            "yellow" => Yellow,
            _ => Black
        };
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is Color c)
            return c.m_value == m_value;
        return false;
    }

    public override int GetHashCode()
    {
        return (int)m_value;
    }

    public static bool operator ==(Color left, Color right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color left, Color right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{R} {G} {B} [{A}]";
    }
}

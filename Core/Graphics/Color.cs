using System;
using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using SixLabors.ImageSharp.PixelFormats;

namespace Helion.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct Color(byte A, byte R, byte G, byte B)
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

    public uint Uint => (uint)((A << 24) | (R << 16) | (G << 8) | B);
    public Vec4F Normalized => new Vec4F(A, R, G, B) * (1 / 255.0f);
    public SixLabors.ImageSharp.Color ToImageSharp => new(new Rgba32(R, G, B, A));

    public Color(Vec4F normalized) : 
        this((byte)(normalized.X * 255), (byte)(normalized.Y * 255), (byte)(normalized.Z * 255), (byte)(normalized.W * 255))
    {
    }

    public Color(byte R, byte G, byte B) : this(255, R, G, B)
    {
    }

    public Color(uint argb) : this(ExtractAlpha(argb), ExtractRed(argb), ExtractGreen(argb), ExtractBlue(argb))
    {
    }

    public static byte ExtractAlpha(uint argb) => (byte)((argb & 0xFF000000) >> 24);
    public static byte ExtractRed(uint argb) => (byte)((argb & 0x00FF0000) >> 16);
    public static byte ExtractGreen(uint argb) => (byte)((argb & 0x0000FF00) >> 8);
    public static byte ExtractBlue(uint argb) => (byte)((argb & 0x000000FF));

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
}

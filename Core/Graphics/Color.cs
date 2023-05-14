using Helion.Geometry.Vectors;
using System;
using System.Runtime.InteropServices;

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
    public static readonly Color DarkGray = (0xA9, 0xA9, 0xA9);
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
    public SixLabors.ImageSharp.Color ToImageSharp => new(new SixLabors.ImageSharp.PixelFormats.Rgba32(R, G, B, A));

    public Color(byte R, byte G, byte B) : this(255, R, G, B)
    {
    }

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

    public static Color FromName(string name)
    { 
        switch (name.ToLower())
        {
        case "black":
            return Color.Black;
        case "blue":
            return Color.Blue;
        case "brown":
            return Color.Brown;
        case "chocolate":
            return Color.Chocolate;
        case "cyan":
            return Color.Cyan;
        case "darkbrown":
            return Color.DarkBrown;
        case "darkgray":
        case "darkgrEy":
            return Color.DarkGray;
        case "darkgreen":
            return Color.DarkGreen;
        case "darkred":
            return Color.DarkRed;
        case "firebrick":
            return Color.Firebrick;
        case "gold":
            return Color.Gold;
        case "gray":
        case "grey":
            return Color.Gray;
        case "green":
            return Color.Green;
        case "khaki":
            return Color.Khaki;
        case "lawngreen":
            return Color.LawnGreen;
        case "lightblue":
            return Color.LightBlue;
        case "lightgreen":
            return Color.LightGreen;
        case "olive":
            return Color.Olive;
        case "orange":
            return Color.Orange;
        case "peachpuff":
            return Color.PeachPuff;
        case "purple":
            return Color.Purple;
        case "red":
            return Color.Red;
        case "rosybrown":
            return Color.RosyBrown;
        case "saddlebrown":
            return Color.SaddleBrown;
        case "tan":
            return Color.Tan;
        case "white":
            return Color.White;
        case "yellow":
            return Color.Yellow;
        default:
            return Color.Black;
        }
    }
}

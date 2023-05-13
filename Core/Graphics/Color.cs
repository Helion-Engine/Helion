using Helion.Geometry.Vectors;
using System;
using System.Runtime.InteropServices;

namespace Helion.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct Color(byte R, byte G, byte B, byte A)
{
    public static readonly Color Transparent = (0, 0, 0, 0);

    public static readonly Color Black = (0, 0, 0);
    public static readonly Color Blue = (0, 0, 255);
    public static readonly Color Brown = (0xFF, 0xFF, 0xFF);
    public static readonly Color Chocolate = (0xFF, 0xFF, 0xFF);
    public static readonly Color Cyan = (0xFF, 0xFF, 0xFF);
    public static readonly Color DarkBrown = (64, 16, 16);
    public static readonly Color DarkGray = (0xFF, 0xFF, 0xFF);
    public static readonly Color DarkGreen = (0xFF, 0xFF, 0xFF);
    public static readonly Color DarkRed = (0xFF, 0xFF, 0xFF);
    public static readonly Color Firebrick = (0xFF, 0xFF, 0xFF);
    public static readonly Color Gold = (0xFF, 0xFF, 0xFF);
    public static readonly Color Gray = (0xFF, 0xFF, 0xFF);
    public static readonly Color Green = (0, 255, 0);
    public static readonly Color Khaki = (0xFF, 0xFF, 0xFF);
    public static readonly Color LawnGreen = (128, 255, 0);
    public static readonly Color LightBlue = (0xFF, 0xFF, 0xFF);
    public static readonly Color LightGreen = (144, 238, 144);
    public static readonly Color Olive = (0xFF, 0xFF, 0xFF);
    public static readonly Color Orange = (0xFF, 0xFF, 0xFF);
    public static readonly Color PeachPuff = (0xFF, 0xFF, 0xFF);
    public static readonly Color Purple = (0xFF, 0xFF, 0xFF);
    public static readonly Color Red = (255, 0, 0);
    public static readonly Color RosyBrown = (0xFF, 0xFF, 0xFF);
    public static readonly Color SaddleBrown = (0xFF, 0xFF, 0xFF);
    public static readonly Color Tan = (0xFF, 0xFF, 0xFF);
    public static readonly Color White = (255, 255, 255);
    public static readonly Color Yellow = (0xFF, 0xFF, 0xFF);

    public Vec4F Normalized => new Vec4F(R, G, B, A) * (1 / 255.0f);
    public SixLabors.ImageSharp.Color ToImageSharp => new(new SixLabors.ImageSharp.PixelFormats.Rgba32(R, G, B, A));

    public Color(byte R, byte G, byte B) : this(R, G, B, 255)
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

    public void Deconstruct(out byte r, out byte g, out byte b, out byte a)
    {
        r = R;
        g = G;
        b = B;
        a = A;
    }

    public static Color FromInts(int r, int g, int b, int a)
    {
        return new((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255), (byte)Math.Clamp(a, 0, 255));
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

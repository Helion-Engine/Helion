using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace Helion.Graphics.ColorSpaces;

// Modified to use helion structs and so the entire dll doesn't need to be pulled in for a few functions:
// https://github.com/muak/ColorMinePortable/blob/ab2c52f4e04c68b7bea55cac46b5a4a8b81802cf/ColorMinePortable/ColorSpaces/Conversions/HslConverter.cs#L6

public class HslConverter
{
    public static Vec3D ToHsl(Color color)
    {
        var max = Math.Max(color.R, Math.Max(color.G, color.B));
        var min = Math.Min(color.R, Math.Min(color.G, color.B));

        if (max == 0 && min == 0)
            return default;

        double h, s, l;
        double diff = max - min;

        //saturation
        var cnt = (max + min) / 2d;
        if (cnt <= 127d)
            s = diff / (max + min);
        else
            s = diff / (510d - diff);

        //lightness
        l = (max + min) / 2d / 255d;

        //hue
        if (diff.ApproxEquals(0))
        {
            h = 0d;
            s = 0d;
        }
        else
        {           

            if (Math.Abs(max - color.R) <= float.Epsilon)
                h = 60d * (color.G - color.B) / diff;
            else if (Math.Abs(max - color.G) <= float.Epsilon)
                h = 60d * (color.B - color.R) / diff + 120d;
            else
                h = 60d * (color.R - color.G) / diff + 240d;

            if (h < 0d)
                h += 360d;
        }

        return new(h, s, l);
    }

    public static Color ToColor(Vec3D hsl)
    {
        var rangedH = hsl.X / 360.0;
        var r = 0.0;
        var g = 0.0;
        var b = 0.0;
        var s = hsl.Y;
        var l = hsl.Z;

        if (!l.ApproxEquals(0))
        {
            if (s.ApproxEquals(0))
            {
                r = g = b = l;
            }
            else
            {
                var q = (l < 0.5) ? l * (1.0 + s) : l + s - (l * s);
                var p = 2.0 * l - q;

                r = GetColorComponent(p, q, rangedH + 1.0 / 3.0);
                g = GetColorComponent(p, q, rangedH);
                b = GetColorComponent(p, q, rangedH - 1.0 / 3.0);
            }
        }

        return new Color((byte)Math.Min(255 * r, 255), (byte)Math.Min(255 * g, 255), (byte)Math.Min(255 * b, 255));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double GetColorComponent(double p, double q, double t)
    {
        if (t < 0.0)
            t += 1.0;
        if (t > 1.0)
            t -= 1.0;

        if (t < 1.0 / 6.0)
            return p + (q - p) * 6.0 * t;

        if (t < 0.5)
            return q;

        if (t < 2.0 / 3.0)
            return p + ((q - p) * ((2.0 / 3.0) - t) * 6.0);

        return p;
    }
}

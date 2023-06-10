using Helion.Graphics;

namespace Helion.Models;

public readonly struct ColorModel
{
    public readonly byte A;
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;

    public ColorModel(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public static ColorModel? ToColorModel(Color? color)
    {
        if (!color.HasValue)
            return null;

        return new ColorModel(color.Value.A, color.Value.R, color.Value.G, color.Value.B);
    }

    public static Color? ToColor(ColorModel? color)
    {
        if (!color.HasValue)
            return null;

        return new Color(color.Value.A, color.Value.R, color.Value.G, color.Value.B);
    }
}

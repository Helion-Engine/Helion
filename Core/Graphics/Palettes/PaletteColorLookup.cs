namespace Helion.Graphics.Palettes;

public class PaletteColorLookup
{
    const int Scale = 8;
    const int ScaleSize = 256 / Scale;
    public readonly byte[] Lookup;

    public PaletteColorLookup(Palette palette)
    {
        Lookup = new byte[ScaleSize * ScaleSize * ScaleSize];

        for (int r = 0; r < ScaleSize; r++)
        {
            for (int g = 0; g < ScaleSize; g++)
            {
                for (int b = 0; b < ScaleSize; b++)
                {
                    var flatIndex = r * ScaleSize * ScaleSize + g * ScaleSize + b;
                    var closest = palette.GetNearestColorIndex((byte)(r * Scale), (byte)(g * Scale), (byte)(b * Scale));
                    Lookup[flatIndex] = closest;
                }
            }
        }
    }

    public byte GetIndex(Color color)
    {
        var flatIndex = (color.R / Scale) * ScaleSize * ScaleSize + (color.G / Scale) * ScaleSize + (color.B / Scale);
        return Lookup[flatIndex];
    }

    public byte GetIndex(byte r, byte g, byte b)
    {
        var flatIndex = (r / Scale) * ScaleSize * ScaleSize + (g / Scale) * ScaleSize + (b / Scale);
        return Lookup[flatIndex];
    }
}

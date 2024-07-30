using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Entries;
using System.Collections.Generic;

namespace Helion.Graphics.Palettes;

public class Colormap
{
    public static readonly int NumColors = 256;
    public static readonly int NumLayers = 34;
    public static readonly int BytesPerColormap = NumColors * NumLayers;
    private static Colormap? DefaultColormap;

    private readonly List<Color[]> layers;
    public readonly bool[] FullBright = new bool[NumColors];

    public readonly Vec3F ColorMix;
    public readonly Entry? Entry;

    public int Count => NumLayers;

    private Colormap(List<Color[]> colormapLayers) 
        : this(colormapLayers, Vec3F.One, null, [])
    {

    }

    private Colormap(List<Color[]> colormapLayers, Vec3F colorMix, Entry? entry, bool[] fullBright)
    {
        layers = colormapLayers;
        ColorMix = colorMix;
        Entry = entry;
        FullBright = fullBright;
    }

    public static Colormap? From(Palette palette, byte[] data, Entry entry)
    {
        if (data.Length < BytesPerColormap)
            return null;

        Vec3I addColors = Vec3I.Zero;
        List<Color[]> colormapLayers = new(NumLayers);
        bool[] fullBright = new bool[NumColors];
        for (int i = 0; i < NumColors; i++)
            fullBright[i] = true;

        var paletteColors = palette.DefaultLayer;
        for (int layer = 0; layer < NumLayers; layer++)
        {
            int startIndex = layer * NumColors;
            var currentColors = new Color[NumColors];
            for (int i = 0; i < NumColors; i++)
            {
                int index = data[startIndex + i];
                if (index < 0 || index >= paletteColors.Length)
                {
                    currentColors[i] = Color.Black;
                    continue;
                }

                var currentColor = paletteColors[data[index]];
                currentColors[i] = currentColor;

                if (layer > 0 && layer < 32)
                {
                    var previousColor = colormapLayers[layer - 1][i];
                    if (previousColor != currentColor)
                        fullBright[i] = false;
                }

                if (layer == 0)
                {
                    addColors.X += currentColor.R;
                    addColors.Y += currentColor.G;
                    addColors.Z += currentColor.B;
                }
            }
            colormapLayers.Add(currentColors);
        }

        var colorMix = addColors.Float / NumColors;
        colorMix.Normalize();
        return new (colormapLayers, colorMix, entry, fullBright);
    }

    public Color[] Layer(int index) => layers[index];

    public static Colormap GetDefaultColormap()
    {
        if (DefaultColormap != null)
            return DefaultColormap;

        List<Color[]> colors = new();
        colors.Add(new Color[NumColors]);
        DefaultColormap = new(colors);
        return DefaultColormap;
    }
}

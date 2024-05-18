using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Entries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.Graphics.Palettes;

public class Colormap
{
    public static readonly int NumColors = 256;
    public static readonly int NumLayers = 34;
    public static readonly int BytesPerColormap = NumColors * NumLayers;
    private static Colormap? DefaultColormap;

    private readonly List<Color[]> layers;

    public readonly Vec3F ColorMix;
    public readonly Entry? Entry;

    public int Count => NumLayers;

    private Colormap(List<Color[]> colormapLayers) 
        : this(colormapLayers, Vec3F.One, null)
    {

    }

    private Colormap(List<Color[]> colormapLayers, Vec3F colorMix, Entry? entry)
    {
        layers = colormapLayers;
        ColorMix = colorMix;
        Entry = entry;
    }

    public static Colormap? From(Palette palette, byte[] data, Entry entry)
    {
        if (data.Length < BytesPerColormap)
            return null;

        Vec3I addColors = Vec3I.Zero;
        List<Color[]> colormapLayers = new();
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

                if (layer != 0)
                    continue;

                var currentColor = paletteColors[data[index]];
                addColors.X += currentColor.R;
                addColors.Y += currentColor.G;
                addColors.Z += currentColor.B;
                currentColors[i] = currentColor;
            }
            colormapLayers.Add(currentColors);
        }

        var colorMix = addColors.Float / NumColors;
        colorMix.Normalize();
        return new (colormapLayers, colorMix, entry);
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

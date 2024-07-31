using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Entries;
using System;
using System.Collections.Generic;

namespace Helion.Graphics.Palettes;

public class Colormap
{
    public static readonly int NumColors = 256;
    public static readonly int NumLayers = 34;
    public static readonly int BytesPerColormap = NumColors * NumLayers;
    private static Colormap? DefaultColormap;

    private readonly List<Color[]> m_layers;
    private readonly List<int[]> m_indexLayers;
    public readonly bool[] FullBright = new bool[NumColors];

    public readonly Vec3F ColorMix;
    public readonly Entry? Entry;

    public int Count => NumLayers;

    private Colormap(List<Color[]> colormapLayers, List<int[]> indices) 
        : this(colormapLayers, indices, Vec3F.One, null, [])
    {

    }

    private Colormap(List<Color[]> colormapLayers, List<int[]> indices, Vec3F colorMix, Entry? entry, bool[] fullBright)
    {
        m_layers = colormapLayers;
        m_indexLayers = indices;
        ColorMix = colorMix;
        Entry = entry;
        FullBright = fullBright;
    }

    public byte GetNearestColorIndex(Color color)
    {
        byte bestIndex = 0;
        var colors = Layer(0);
        float nearest = int.MaxValue;
        for (int i = 0; i < colors.Length; i++)
        {
            var paletteColor = colors[i];
            var value = paletteColor.Normalized - color.Normalized;
            float calc = Math.Abs(value.X) + Math.Abs(value.Y) + Math.Abs(value.Z);
            if (calc < nearest)
            {
                bestIndex = (byte)i;
                nearest = calc;
            }
        }

        return bestIndex;
    }

    public static Colormap? From(Palette palette, byte[] data, Entry entry)
    {
        if (data.Length < BytesPerColormap)
            return null;

        Vec3I addColors = Vec3I.Zero;
        List<Color[]> colormapLayers = new(NumLayers);
        List<int[]> colormapLayerIndices = new(NumLayers);
        bool[] fullBright = new bool[NumColors];
        for (int i = 0; i < NumColors; i++)
            fullBright[i] = true;

        var paletteColors = palette.DefaultLayer;
        for (int layer = 0; layer < NumLayers; layer++)
        {
            int startIndex = layer * NumColors;
            var currentColors = new Color[NumColors];
            var currentIndices = new int[NumColors];
            for (int i = 0; i < NumColors; i++)
            {
                int index = data[startIndex + i];
                currentIndices[i] = index;
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
            colormapLayerIndices.Add(currentIndices);
        }

        var colorMix = addColors.Float / NumColors;
        colorMix.Normalize();
        return new(colormapLayers, colormapLayerIndices, colorMix, entry, fullBright);
    }

    public Color[] Layer(int index) => m_layers[index];

    public int[] IndexLayer(int index) => m_indexLayers[index];

    public static Colormap GetDefaultColormap()
    {
        if (DefaultColormap != null)
            return DefaultColormap;

        List<Color[]> colors = [new Color[NumColors]];
        List<int[]> indices = [new int[NumColors]];

        DefaultColormap = new(colors, indices);
        return DefaultColormap;
    }
}

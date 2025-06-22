using ColorMine.ColorSpaces;
using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Entries;
using System;
using System.Collections.Generic;

namespace Helion.Graphics.Palettes;

public enum TranslateColor { Gray, Brown, Red, Count }

public class Colormap
{
    public const int NumColors = 256;
    public const int NumLayers = 34;
    public const int BytesPerColormap = NumColors * NumLayers;
    private static Colormap? DefaultColormap;

    private readonly List<Color[]> m_layers;
    private readonly List<byte[]> m_indexLayers;
    public readonly bool[] FullBright = new bool[NumColors];


    public int Index;
    public Vec3F ColorMix;
    public readonly Entry? Entry;

    public int Count => m_layers.Count;

    private Colormap(List<Color[]> colormapLayers, List<byte[]> indices) 
        : this(colormapLayers, indices, Vec3F.One, null, [])
    {

    }

    private Colormap(List<Color[]> colormapLayers, List<byte[]> indices, Vec3F colorMix, Entry? entry, bool[] fullBright)
    {
        m_layers = colormapLayers;
        m_indexLayers = indices;
        ColorMix = colorMix;
        Entry = entry;
        FullBright = fullBright;
    }

    public static Colormap? From(Palette palette, byte[] data, Entry? entry)
    {
        // Must have at least one layer
        if (data.Length < NumColors)
            return null;

        Vec3I addColors = Vec3I.Zero;
        List<Color[]> colormapLayers = new(NumLayers);
        List<byte[]> colormapLayerIndices = new(NumLayers);
        bool[] fullBright = new bool[NumColors];
        for (int i = 0; i < NumColors; i++)
            fullBright[i] = true;

        var paletteColors = palette.DefaultLayer;
        for (int layer = 0; layer < NumLayers; layer++)
        {
            int startIndex = layer * NumColors;
            if (startIndex + NumColors > data.Length)
                break;

            var currentColors = new Color[NumColors];
            var currentIndices = new byte[NumColors];
            for (int i = 0; i < NumColors; i++)
            {
                var index = data[startIndex + i];
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
                    if (previousColor.Uint != currentColor.Uint)
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

    public Color[] Layer(int index)
    {
        if (index >= m_layers.Count)
            return m_layers[0];
        return m_layers[index];
    }

    public byte[] IndexLayer(int index)
    {
        if (index >= m_indexLayers.Count)
            return m_indexLayers[0];
        return m_indexLayers[index];
    }

    public static Colormap GetDefaultColormap()
    {
        if (DefaultColormap != null)
            return DefaultColormap;

        List<Color[]> colors = [new Color[NumColors]];
        List<byte[]> indices = [new byte[NumColors]];

        DefaultColormap = new(colors, indices);
        return DefaultColormap;
    }

    public static Colormap? CreateTranslatedColormap(Palette palette, byte[] colorMap, TranslateColor color)
    {
        var translated = TranslateIndices(colorMap, color);
        return From(palette, translated, null);
    }

    public static Colormap? TranslateToNearestMatch(Palette palette, byte[] colorMap, PaletteColor translateColor)
    {
        var translated = TranslateIndicesNearest(palette, colorMap, translateColor);
        return From(palette, translated, null);
    }

    private static byte[] TranslateIndicesNearest(Palette palette, byte[] colorMap, PaletteColor translateColor)
    {
        var hsl = ToHsl(translateColor);
        var translate = new byte[colorMap.Length];
        var lookup = new Dictionary<Color, byte>();
        var colors = palette.Layer(0);
        int dataIndex = 0;

        for (int layer = 0; layer < NumLayers; layer++)
        {
            for (int colorIndex = 0; colorIndex < NumColors; colorIndex++, dataIndex++)
            {
                if (dataIndex >= translate.Length)
                    return translate;

                var color = colors[colorMap[dataIndex]];
                if (lookup.TryGetValue(color, out var value))
                {
                    translate[dataIndex] = value;
                    continue;
                }

                var newIndex = ShiftToHsl(palette, color, hsl);
                translate[dataIndex] = newIndex;
                lookup[color] = newIndex;
            }
        }
        return translate;
    }

    private static HslShift ToHsl(PaletteColor paletteColor)
    {
        return paletteColor switch
        {
            PaletteColor.Blue => new(260, null, null, 0.1),
            PaletteColor.Yellow => new(60, null, null, 0.1),
            PaletteColor.Orange => new(30, null, null, 0),
            PaletteColor.Purple => new(280, null, null, 0),
            PaletteColor.Green => new(100, null, null, 0),
            PaletteColor.Gray => new(null, 0, null, 0),
            PaletteColor.Black => new(null, 0, null, -0.2),
            PaletteColor.White => new(null, 0, null, 0.5),
            _ => new(null, null, null, 0),
        };
    }

    private static byte ShiftToHsl(Palette palette, Color color, HslShift toHsl)
    {
        var rgb = new Rgb { R = color.R, G = color.G, B = color.B };
        var hsl = rgb.To<Hsl>();

        if (toHsl.H.HasValue)
            hsl.H = toHsl.H.Value;
        if (toHsl.S.HasValue)
            hsl.S = toHsl.S.Value;
        if (toHsl.L.HasValue)
            hsl.L = toHsl.L.Value;

        hsl.L = Math.Clamp(hsl.L + toHsl.AddL, 0, 1);

        var newRgb = hsl.To<Rgb>();
        var newColor = new Color((byte)newRgb.R, (byte)newRgb.G, (byte)newRgb.B);
        return palette.GetNearestColorIndex(newColor);
    }

    public static Colormap? CreateTranslatedColormap(Palette palette, byte[] colorMap, byte[] translateTable)
    {
        if (translateTable.Length < 256)
            return null;

        var translated = TranslateIndices(colorMap, translateTable);
        return From(palette, translated, null);
    }

    private static byte[] TranslateIndices(byte[] data, TranslateColor color)
    {
        byte offset = color switch
        {
            TranslateColor.Gray => 0x60,
            TranslateColor.Brown => 0x40,
            TranslateColor.Red => 0x20,
            _ => 0
        };

        var translate = new byte[data.Length];
        int index = 0;
        for (int layer = 0; layer < NumLayers; layer++)
        {
            for (int colorIndex = 0; colorIndex < NumColors; colorIndex++, index++)
            {
                if (index >= translate.Length)
                    return translate;

                int dataIndex = (layer * NumColors) + offset + (colorIndex & 0xF);
                // Only translate green color indices
                if (colorIndex >= 0x70 && colorIndex <= 0x7f && dataIndex < data.Length)
                    translate[index] = data[dataIndex];
                else
                    translate[index] = data[index];
            }
        }

        return translate;
    }

    private static byte[] TranslateIndices(byte[] data, byte[] translateTable)
    {
        var translate = new byte[data.Length];
        int index = 0;
        for (int layer = 0; layer < NumLayers; layer++)
        {
            for (int colorIndex = 0; colorIndex < NumColors; colorIndex++, index++)
            {
                if (index >= translate.Length)
                    return translate;

                int dataIndex = (layer * NumColors) + translateTable[colorIndex];
                translate[index] = data[dataIndex];
            }
        }

        return translate;
    }
}

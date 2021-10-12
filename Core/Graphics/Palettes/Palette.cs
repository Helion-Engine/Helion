using System;
using System.Collections.Generic;
using System.Drawing;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Palettes;

/// <summary>
/// An encapsulation of a palette. Each palette is made up of one or more
/// layers, where a palette layer is an array of all 256 RGB colors. The
/// top layer (index 0) is the main layer used for drawing images, and the
/// rest are for vanilla blood coloring or tinting.
/// </summary>
public class Palette
{
    public static readonly int NumColors = 256;
    public static readonly int ColorComponents = 3;
    public static readonly int BytesPerLayer = NumColors * ColorComponents;
    private static Palette? DefaultPalette;

    private readonly List<Color[]> layers;

    public int Count => layers.Count;
    public Color[] DefaultLayer => layers[0];

    private Palette(List<Color[]> paletteLayers)
    {
        layers = paletteLayers;
    }

    /// <summary>
    /// Attempts to create a palette from the provided data. If the data is
    /// a valid palette, it will return the created instance. Otherwise if
    /// the data is corrupt/wrong, an empty optional is returned.
    /// </summary>
    /// <param name="data">The data to attempt to make the palette from.
    /// </param>
    /// <returns>A palette if successful, an empty optional otherwise.
    /// </returns>
    public static Palette? From(byte[] data)
    {
        if (data.Length == 0 || data.Length % BytesPerLayer != 0)
            return null;

        List<Color[]> paletteLayers = new();
        for (int layer = 0; layer < data.Length / BytesPerLayer; layer++)
        {
            int offset = layer * BytesPerLayer;
            Span<byte> layerSpan = new Span<byte>(data, offset, BytesPerLayer);
            paletteLayers.Add(PaletteLayerFrom(layerSpan));
        }

        return new Palette(paletteLayers);
    }

    private static Color[] PaletteLayerFrom(Span<byte> data)
    {
        Precondition(data.Length == BytesPerLayer, "Palette byte span range incorrect");

        Color[] paletteColors = new Color[NumColors];

        int offset = 0;
        for (int i = 0; i < BytesPerLayer; i += ColorComponents)
            paletteColors[offset++] = Color.FromArgb(255, data[i], data[i + 1], data[i + 2]);

        return paletteColors;
    }

    /// <summary>
    /// Gets the palette layer from the index provided. For example, if a
    /// palette has 12 layers, then palette[0] would get the topmost layer.
    /// </summary>
    /// <param name="index">The palette index to get. This should be in the
    /// range of [0, Count).</param>
    /// <returns>The palette layer.</returns>
    public Color[] Layer(int index) => layers[index];

    /// <summary>
    /// Gets a default palette if one doesn't exist.
    /// </summary>
    /// <returns>The default palette.</returns>
    public static Palette GetDefaultPalette()
    {
        if (DefaultPalette != null)
            return DefaultPalette;

        byte[] data = new byte[NumColors * ColorComponents];

        for (int i = 0; i < NumColors; i++)
        {
            data[i] = (byte)i;
            data[i + 1] = (byte)i;
            data[i + 2] = (byte)i;
        }

        Palette? palette = From(data);
        if (palette == null)
            throw new NullReferenceException("Failed to create the default palette, shouldn't be possible");

        DefaultPalette = palette;
        return palette;
    }
}


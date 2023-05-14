using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.Graphics.Palettes;

/// <summary>
/// An encapsulation of a colormap. Each colormap is made up of one or more
/// layers, where a colormap layer is an array of all 256 RGB colors. The
/// top layer (index 0) is the main layer used for drawing images, and the
/// rest are for vanilla blood coloring or tinting.
/// </summary>
public class Colormap
{
    public static readonly int NumColors = 256;
    public static readonly int ColorComponents = 3;
    public static readonly int NumLayers = 34;
    public static readonly int BytesPerLayer = NumColors * ColorComponents;
    public static readonly int BytesPerColormap = BytesPerLayer * NumLayers;
    private static Colormap? DefaultColormap;

    private readonly List<Color[]> layers;

    public int Count => NumLayers;

    private Colormap(List<Color[]> colormapLayers)
    {
        layers = colormapLayers;
    }

    public static Colormap? From(byte[] data)
    {
        if (data.Length != BytesPerColormap)
            return null;

        List<Color[]> colormapLayers = new();
        for (int layer = 0; layer < data.Length / BytesPerLayer; layer++)
        {
            int offset = layer * BytesPerLayer;
            Span<byte> layerSpan = new(data, offset, BytesPerLayer);
            colormapLayers.Add(ColormapLayerFrom(layerSpan));
        }

        return new(colormapLayers);
    }

    private static Color[] ColormapLayerFrom(Span<byte> data)
    {
        Debug.Assert(data.Length == BytesPerLayer, "Colormap byte span range incorrect");

        Color[] colormapColors = new Color[NumColors];

        int offset = 0;
        for (int i = 0; i < BytesPerLayer; i += ColorComponents)
            colormapColors[offset++] = Color.FromInts(255, data[i], data[i + 1], data[i + 2]);

        return colormapColors;
    }

    public Color[] Layer(int index) => layers[index];

    public static Colormap GetDefaultColormap()
    {
        if (DefaultColormap != null)
            return DefaultColormap;

        byte[] data = new byte[BytesPerColormap];

        for (int i = 0; i < BytesPerColormap / ColorComponents; i++)
        {
            data[i] = (byte)i;
            data[i + 1] = (byte)i;
            data[i + 2] = (byte)i;
        }

        Colormap? colormap = From(data) ?? throw new("Failed to create the default colormap, shouldn't be possible");

        DefaultColormap = colormap;
        return colormap;
    }
}

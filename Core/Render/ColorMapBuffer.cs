using Helion.Graphics.Palettes;
using System.Collections.Generic;

namespace Helion.Render;

public static class ColorMapBuffer
{
    const int LayerSize = Colormap.NumColors * Colormap.NumLayers * 3;
    const int ColorMapSize = LayerSize * Palette.NumPalettes;

    public static float[] Create(Palette palette, Colormap baseColormap, List<Colormap> colormaps)
    {
        float[] buffer = new float[ColorMapSize * (colormaps.Count + 1)];
        WriteColorMap(0, palette, baseColormap, buffer);

        for (int i = 0; i < colormaps.Count; i++)
        {
            var colormap = colormaps[i];
            colormap.Index = i + 1;
            WriteColorMap(i + 1, palette, colormaps[i], buffer);
        }

        return buffer;
    }

    private static int WriteColorMap(int index, Palette palettes, Colormap colormap, float[] buffer)
    {
        int startOffset = index * ColorMapSize;
        int offset = startOffset;
        for (int paletteIndex = 0; paletteIndex < palettes.Count && paletteIndex < Palette.NumPalettes; paletteIndex++)
        {
            var palette = palettes.Layer(paletteIndex);
            for (int colormapIndex = 0; colormapIndex < colormap.Count && colormapIndex < Colormap.NumColors; colormapIndex++)
            {
                var layer = colormap.IndexLayer(colormapIndex);
                for (int i = 0; i < layer.Length; i++)
                {
                    var color = palette[layer[i]];
                    buffer[offset++] = color.R / 255f;
                    buffer[offset++] = color.G / 255f;
                    buffer[offset++] = color.B / 255f;
                }
            }
        }

        return offset;
    }
}

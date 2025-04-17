using System;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public static class VertexOptions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float World(float topLeft, float alpha, float addAlpha, int lightLevelBufferIndex)
    {
        // Alpha must be first since it's < 1
        return alpha + (topLeft * 2) + (addAlpha * 4) + (lightLevelBufferIndex * 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ColorMapIndex(int colorMapIndex, int vertexLightLevel)
    {
        // Packs the vertexLightLevel used for UDMF with the colormap index.
        // The static vertex is on the edge of the optimal size for performance so this prevents adding another float prop.
        return vertexLightLevel + 256 * colorMapIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LightLevelAdd(int mapId, float lightLevelAdd)
    {
        // Packs LightLevelAdd with MapId
        if (lightLevelAdd < 0)
            return -(Math.Abs(lightLevelAdd) + 256 * mapId);

        return lightLevelAdd + 256 * mapId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Entity(float alpha, float fuzz, float flipU, float colormap)
    {
        return alpha + (fuzz * 2) + (flipU * 4) + (colormap * 8);
    }
}

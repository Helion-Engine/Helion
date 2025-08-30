using System;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public static class VertexOptions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float World(float topLeft, float alpha, float addAlpha, int upper, int lower, int lightLevelBufferIndex)
    {
        // Alpha must be first since it's < 1
        return alpha + (topLeft * 2) + (addAlpha * 4) + (lower * 8) + (upper * 16) + (lightLevelBufferIndex * 32);
    }

    public static float ColorMapIndex(int colorMapIndex, int vertexLightLevel)
    {
        // First 8 bits are lightLevel, next 24 are colorMapIndex
        int packed = ((colorMapIndex & 0xFFFFFF) << 8) | ((vertexLightLevel) & 0xFF);
        return BitConverter.Int32BitsToSingle(packed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LightLevelAdd(int mapId, int lightLevelAdd)
    {
        // Packs LightLevelAdd with MapId
        // First bit is sign, next 8 are lightLevelAdd, last 23 are mapId
        if (lightLevelAdd < 0)
            return PackLightLevelAddAndMapId(mapId, Math.Abs(lightLevelAdd), 1);

        return PackLightLevelAddAndMapId(mapId, lightLevelAdd, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float PackLightLevelAddAndMapId(int mapId, int lightLevelAdd, int signFlag)
    {
        int packed = ((mapId & 0xFFFFFF) << 9) | ((lightLevelAdd & 0xFF) << 1) | signFlag;
        return BitConverter.Int32BitsToSingle(packed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Entity(float alpha, float fuzz, float flipU, float colormap)
    {
        return alpha + (fuzz * 2) + (flipU * 4) + (colormap * 8);
    }
}

using System;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public static class VertexOptions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float World(int topLeft, float alpha, int addAlpha, int upper, int lower, int lightLevelBufferIndex)
    {
        int alphaByte = (int)(alpha * 255.0f);
        int packed = (alphaByte & 0xFF) | (topLeft << 8) | (addAlpha << 9) | (upper << 10) | (lower << 11) | (lightLevelBufferIndex << 12);
        return BitConverter.Int32BitsToSingle(packed);
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
    public static float Entity(float alpha, int fuzz, int flipU, int colormap)
    {
        int alphaByte = (int)(alpha * 255.0f);
        int packed = (alphaByte & 0xFF) | (fuzz << 8) | (flipU << 9) | (colormap << 10);
        return BitConverter.Int32BitsToSingle(packed);
    }
}

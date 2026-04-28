using System;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public static class VertexOptions
{
    // When overrideLightIndex is non-zero then lighting uses index overrideLightIndex - 1
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe float PackSurface(int topLeft, float alpha, int addAlpha, int upper, int lower, int overrideLightIndex)
    {
        int alphaByte = (int)(alpha * 255.0f);
        int packed = (alphaByte & 0xFF) | (topLeft << 8) | (addAlpha << 9) | (upper << 10) | (lower << 11) | (overrideLightIndex << 12);
        return *(float*)&packed;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // UvFlags only required for walls
    public static unsafe float PackRender(int lightIndex, int vertexLightLevel, UvFlags uvFlags = UvFlags.Normal)
    {
        // lightIndex 22 bits, uvFlags 2 bits, vertexLightLevel 8 bits
        int packed = ((lightIndex & 0x3FFFFF) << 10) | (((int)uvFlags & 0x3) << 8) | (vertexLightLevel & 0xFF);
        return *(float*)&packed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe float LightLevelAdd(int mapId, int lightLevelAdd)
    {
        // Packs LightLevelAdd with MapId
        // First bit is sign, next 8 are lightLevelAdd, last 23 are mapId
        int signMask = lightLevelAdd >> 31;
        lightLevelAdd = (lightLevelAdd ^ signMask) - signMask;
        int packed = ((mapId & 0xFFFFFF) << 9) | ((lightLevelAdd & 0xFF) << 1) | (signMask & 1);
        return *(float*)&packed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe float EntityPackSurface(float alpha, int fuzz, int flipU, int colormap, int lightLevel)
    {
        int alphaByte = (int)(alpha * 255.0f);
        int packed = (alphaByte & 0xFF) | (fuzz << 8) | (flipU << 9) | (Math.Clamp(lightLevel, 0, 255) << 10) | (colormap << 18);
        return *(float*)&packed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe float EntityPackRender(int lightIndex, int renderIndex)
    {
        int packed = (lightIndex << 12) | (renderIndex & 0xFFF);
        return *(float*)&packed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe float EntityPackXYZ(int offsetXY, int offsetZ)
    {
        // Shift negative bit for mask to get absolute value and sign bit to remove branches
        int maskXY = offsetXY >> 31;
        int maskZ = offsetZ >> 31;
        offsetXY = (offsetXY ^ maskXY) - maskXY;
        offsetZ = (offsetZ ^ maskZ) - maskZ;
        int offsetXYSign = maskXY & 1;
        int offsetZSign = maskZ & 1;

        int packed = (offsetXYSign << 31) | (offsetZSign << 30) | (offsetXY << 16) | offsetZ;
        return *(float*)&packed;
    }
}

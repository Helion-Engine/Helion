using System;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public static class VertexOptions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float World(float alpha, float addAlpha, int lightLevelBufferIndex)
    {
        return alpha + (addAlpha * 2) + (lightLevelBufferIndex * 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Entity(float alpha, float fuzz, float flipU, float colormap)
    {
        return alpha + (fuzz * 2) + (flipU * 4) + (colormap * 8);
    }
}

using System;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public struct VertexOptionsOut
{
    public float LightBufferIndex;
    public float AddAlpha;
    public float Alpha;
    public float Left;
    public float Top;
}

public static class VertexOptions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float World(float top, float left, float alpha, float addAlpha, int lightLevelBufferIndex)
    {
        return left + (top * 2) + (alpha * 4) + (addAlpha * 8) + (lightLevelBufferIndex * 16);
    }    

    public static VertexOptionsOut GetOptions(float options)
    {
        float splitOptions = options;
        float lightLevelBufferIndex = (float)Math.Floor(splitOptions / 16);
        splitOptions -= (lightLevelBufferIndex * 16);
        float addAlphaFrag = (float)Math.Floor(splitOptions / 8);
        splitOptions -= (addAlphaFrag * 8);
        float alphaFrag = (float)Math.Floor(splitOptions / 4);
        splitOptions -= (alphaFrag * 4);
        float leftFrag = (float)Math.Floor(splitOptions / 2);
        float topFrag = splitOptions - (leftFrag * 2);

        return new VertexOptionsOut()
        {
            LightBufferIndex = lightLevelBufferIndex,
            AddAlpha = addAlphaFrag,
            Alpha = alphaFrag,
            Left = leftFrag,
            Top = topFrag
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ColorMapIndex(int colorMapIndex, int vertexLightLevel)
    {
        // Packs the vertexLightLevel used for UDMF with the colormap index.
        // The static vertex is on the edge of the optimal size for performance so this prevents adding another float prop.
        return vertexLightLevel + 256 * colorMapIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Entity(float alpha, float fuzz, float flipU, float colormap)
    {
        return alpha + (fuzz * 2) + (flipU * 4) + (colormap * 8);
    }
}

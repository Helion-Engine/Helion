using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public static class VertexOptions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Get(float alpha, float addAlpha, int lightLevelBufferIndex)
    {
        return alpha + (addAlpha * 2) + (lightLevelBufferIndex * 4);
    }
}

using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct HudVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float U;
        public readonly float V;
        public readonly float Alpha;

        public HudVertex(float x, float y, float z, float u, float v, float alpha)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            Alpha = alpha;
        }
    }
}
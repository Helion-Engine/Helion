using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct HudVertex
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public readonly int U;
        public readonly int V;
        public readonly float Alpha;

        public HudVertex(int x, int y, int z, int u, int v, float alpha)
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
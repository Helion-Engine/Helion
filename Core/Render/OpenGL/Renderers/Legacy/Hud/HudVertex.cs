using System.Drawing;
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
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte RgbBlend;
        public readonly float Alpha;

        public HudVertex(float x, float y, float z, float u, float v, byte r, byte g, byte b, 
            byte rgbBlend, float alpha)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            R = r;
            G = g;
            B = b;
            RgbBlend = rgbBlend;
            Alpha = alpha;
        }

        public HudVertex(float x, float y, float z, float u, float v, Color color, float alpha) :
            this(x, y, z, u, v, color.R, color.G, color.B, color.A, alpha)
        {
        }
    }
}
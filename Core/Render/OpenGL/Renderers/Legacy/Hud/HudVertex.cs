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
        public readonly byte MultiplierR;
        public readonly byte MultiplierG;
        public readonly byte MultiplierB;
        public readonly byte MultiplierFactor;
        public readonly float Alpha;

        public HudVertex(float x, float y, float z, float u, float v, byte multiplierR, byte multiplierG,
            byte multiplierB, byte multiplierFactor, float alpha)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            MultiplierR = multiplierR;
            MultiplierG = multiplierG;
            MultiplierB = multiplierB;
            MultiplierFactor = multiplierFactor;
            Alpha = alpha;
        }

        public HudVertex(float x, float y, float z, float u, float v, Color multiplierColor, float alpha) :
            this(x, y, z, u, v, multiplierColor.R, multiplierColor.G, multiplierColor.B, multiplierColor.A, alpha)
        {
        }
    }
}
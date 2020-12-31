using System.Drawing;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LegacyVertex
    {
        public float X;
        public float Y;
        public float Z;
        public float U;
        public float V;
        public float LightLevelUnit;
        public float Alpha;
        public float R;
        public float G;
        public float B;

        public LegacyVertex(float x, float y, float z, float u, float v, short lightLevel = 256, float alpha = 1.0f) :
            this(x, y, z, u, v, Color.White, lightLevel, alpha)
        {
        }

        public LegacyVertex(float x, float y, float z, float u, float v, Color color, short lightLevel = 256, float alpha = 1.0f)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            LightLevelUnit = lightLevel / 256.0f;
            Alpha = alpha;
            R = color.R / 255.0f;
            G = color.G / 255.0f;
            B = color.B / 255.0f;
        }
    }
}
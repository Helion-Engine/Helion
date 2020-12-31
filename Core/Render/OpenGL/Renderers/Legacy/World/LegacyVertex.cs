using System.Runtime.InteropServices;
using Helion.Render.Shared.World;

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

        public LegacyVertex(float x, float y, float z, float u, float v, short lightLevel, float alpha = 1.0f)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            LightLevelUnit = lightLevel / 256.0f;
            Alpha = alpha;
        }
    }
}
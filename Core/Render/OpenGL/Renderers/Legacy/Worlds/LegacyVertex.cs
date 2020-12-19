using System.Runtime.InteropServices;
using Helion.Render.Shared.Worlds;

namespace Helion.Render.OpenGL.Renderers.Legacy.Worlds
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

        public LegacyVertex(float x, float y, float z, float u, float v, short lightLevel)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            LightLevelUnit = lightLevel / 256.0f;
        }

        public LegacyVertex(WorldVertex vertex, short lightLevel) :
            this(vertex.X, vertex.Y, vertex.Z, vertex.U, vertex.V, lightLevel)
        {
        }
    }
}
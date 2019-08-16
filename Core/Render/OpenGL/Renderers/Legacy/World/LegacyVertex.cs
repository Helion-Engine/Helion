using System.Runtime.InteropServices;
using Helion.Render.Shared.World;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LegacyVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float U;
        public readonly float V;
        public readonly float LightLevelUnit;

        public LegacyVertex(float x, float y, float z, float u, float v, byte lightLevel)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            LightLevelUnit = lightLevel / 255.0f;
        }

        public LegacyVertex(WorldVertex vertex, byte lightLevel) :
            this(vertex.X, vertex.Y, vertex.Z, vertex.U, vertex.V, lightLevel)
        {
        }
    }
}
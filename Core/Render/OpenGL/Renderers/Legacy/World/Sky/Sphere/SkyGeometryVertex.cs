using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SkyGeometryVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public SkyGeometryVertex(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public SkyGeometryVertex(LegacyVertex vertex) : this(vertex.X, vertex.Y, vertex.Z)
        {
        }
    }
}
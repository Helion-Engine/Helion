using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Primitives;

namespace Helion.Render.OpenGL.Renderers.Hud
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct GLHudVertex
    {
        public readonly Vec3F Pos;
        public readonly ByteColor Color;
        public readonly float Alpha;

    }
}

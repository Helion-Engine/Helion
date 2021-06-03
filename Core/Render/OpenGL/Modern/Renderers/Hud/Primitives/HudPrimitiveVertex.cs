using System.Runtime.InteropServices;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Attributes;
using Helion.Render.OpenGL.Primitives;

namespace Helion.Render.OpenGL.Modern.Renderers.Hud.Primitives
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct HudPrimitiveVertex
    {
        public readonly vec3 Pos;
        [Normalized]
        public readonly ByteColor Rgba;

        public HudPrimitiveVertex(Vec3F pos, ByteColor rgba)
        {
            Pos = pos.GlmVector;
            Rgba = rgba;
        }
    }
}

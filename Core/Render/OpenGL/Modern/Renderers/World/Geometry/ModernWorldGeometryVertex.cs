using System.Runtime.InteropServices;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Attributes;
using Helion.Render.OpenGL.Primitives;

namespace Helion.Render.OpenGL.Modern.Renderers.World.Geometry
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct ModernWorldGeometryVertex
    {
        public readonly vec3 Pos;
        public readonly vec2 UV;
        public readonly BindlessHandle Handle;
        [Normalized]
        public readonly ByteColor RgbaScale;
        public readonly float Alpha;

        public ModernWorldGeometryVertex(Vec3F pos, Vec2F uv, BindlessHandle handle, ByteColor rgbaScale, float alpha)
        {
            Pos = pos.GlmVector;
            UV = new vec2(uv.U, uv.V);
            Handle = handle;
            RgbaScale = rgbaScale;
            Alpha = alpha;
        }
    }
}

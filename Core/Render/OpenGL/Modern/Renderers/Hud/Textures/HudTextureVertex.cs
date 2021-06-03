using System.Runtime.InteropServices;
using GlmSharp;
using Helion.Render.OpenGL.Attributes;
using Helion.Render.OpenGL.Primitives;

namespace Helion.Render.OpenGL.Modern.Renderers.Hud.Textures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct HudTextureVertex
    {
        public readonly vec3 Pos;
        public readonly vec2 UV;
        [Normalized]
        public readonly ByteColor Rgba;
        public readonly float Alpha;
        public readonly BindlessHandle Handle;

        public HudTextureVertex(vec3 pos, vec2 uv, ByteColor rgba, float alpha, BindlessHandle handle)
        {
            Pos = pos;
            UV = uv;
            Rgba = rgba;
            Alpha = alpha;
            Handle = handle;
        }
    }
}

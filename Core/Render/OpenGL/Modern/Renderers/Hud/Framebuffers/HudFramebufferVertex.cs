using System.Runtime.InteropServices;
using GlmSharp;

namespace Helion.Render.OpenGL.Modern.Renderers.Hud.Framebuffers
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public readonly struct HudFramebufferVertex
    {
        public readonly vec3 Pos;
        public readonly vec2 UV;

        public HudFramebufferVertex(vec3 pos, vec2 uv)
        {
            Pos = pos;
            UV = uv;
        }
    }
}

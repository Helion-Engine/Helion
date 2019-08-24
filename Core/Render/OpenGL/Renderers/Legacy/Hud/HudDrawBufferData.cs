using System.Collections.Generic;
using Helion.Render.OpenGL.Texture;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud
{
    public class HudDrawBufferData
    {
        public readonly GLTexture Texture;
        public readonly List<HudVertex> Vertices = new List<HudVertex>();

        public HudDrawBufferData(GLTexture texture)
        {
            Texture = texture;
        }
    }
}
using System.Collections.Generic;
using Helion.Render.OpenGL.Textures.Legacy;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud
{
    public class HudDrawBufferData
    {
        public readonly GLLegacyTexture Texture;
        public readonly List<HudVertex> Vertices = new List<HudVertex>();

        public HudDrawBufferData(GLLegacyTexture texture)
        {
            Texture = texture;
        }
    }
}
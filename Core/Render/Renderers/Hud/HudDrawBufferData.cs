using System.Collections.Generic;
using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Renderers.Hud;

namespace Helion.Render.Renderers.Hud;

public class HudDrawBufferData
{
    public GLLegacyTexture Texture;
    public readonly List<HudVertex> Vertices = new();

    public HudDrawBufferData(GLLegacyTexture texture)
    {
        Texture = texture;
    }

    public void Set(GLLegacyTexture texture)
    {
        Texture = texture;
    }
}

using System.Collections.Generic;
using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Renderers;
using Helion.Render.Legacy.Renderers.Hud;
using Helion.Render.Legacy.Texture.Legacy;

namespace Helion.Render.Legacy.Renderers.Hud;

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

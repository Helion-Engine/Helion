using System.Collections.Generic;
using Helion.Render.Legacy.Texture.Legacy;

namespace Helion.Render.Legacy.Renderers.Legacy.Hud;

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

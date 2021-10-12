using System.Collections.Generic;
using Helion.Render.Legacy.Texture.Legacy;

namespace Helion.Render.Legacy.Renderers.Legacy.Hud;

public class HudDrawBufferData
{
    public readonly GLLegacyTexture Texture;
    public readonly List<HudVertex> Vertices = new List<HudVertex>();

    public HudDrawBufferData(GLLegacyTexture texture)
    {
        Texture = texture;
    }
}


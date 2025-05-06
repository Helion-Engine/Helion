using System.Collections.Generic;
using Helion.Render.OpenGL.Texture.Legacy;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud;

public class HudDrawBufferData(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
{
    public GLLegacyTexture Texture = texture;
    public GLLegacyTexture? BrightmapTexture = brightmapTexture;
    public readonly List<HudVertex> Vertices = [];

    public void Set(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        Texture = texture;
        BrightmapTexture = brightmapTexture;
    }
}

using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud;

public class HudDrawBufferData(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
{
    public GLLegacyTexture Texture = texture;
    public GLLegacyTexture? BrightmapTexture = brightmapTexture;
    public readonly DynamicArray<HudVertex> Vertices = new(128, arrayPool: true);

    public void Set(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        Texture = texture;
        BrightmapTexture = brightmapTexture;
    }
}

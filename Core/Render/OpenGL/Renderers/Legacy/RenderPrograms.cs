using Helion.Render.OpenGL.Renderers.Legacy.Hud;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy;

public sealed class RenderPrograms
{
    internal WorldRenderPrograms WorldRenderPrograms = new();
    internal RenderProgramHandle<LegacyHudShader> HudProgram = new(new LegacyHudShader());

    public void RecreateWorld()
    {
        WorldRenderPrograms.Recreate();
    }

    public void RecreateHud()
    {
        HudProgram.Recreate();
    }
}

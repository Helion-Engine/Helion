using Helion.Geometry;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

namespace Helion.Render.Common.Context;

public class HudRenderContext
{
    private static int id;
    public int Id = id++;
    public Dimension Dimension;
    public bool DrawColorMap;
    public bool DrawFuzz;
    public bool DrawPalette;

    public HudRenderContext(Dimension dimension)
    {
        Dimension = dimension;
    }

    public void Set(Dimension dimension)
    {
        Dimension = dimension;
        DrawColorMap = ShaderVars.PaletteColorMode;
        DrawFuzz = false;
        DrawPalette = true;
    }
}

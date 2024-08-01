using Helion.Geometry;

namespace Helion.Render.Common.Context;

public class HudRenderContext
{
    private static int id;
    public int Id = id++;
    public Dimension Dimension;
    public bool DrawInvul;
    public bool DrawFuzz;
    public bool DrawColorMap;

    public HudRenderContext(Dimension dimension)
    {
        Dimension = dimension;
    }

    public void Set(Dimension dimension)
    {
        Dimension = dimension;
        DrawInvul = false;
        DrawFuzz = false;
        DrawColorMap = true;
    }
}

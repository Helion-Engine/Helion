using Helion.Geometry;

namespace Helion.Render.Common.Context;

public class HudRenderContext
{
    public Dimension Dimension;
    public bool DrawInvul;

    public HudRenderContext(Dimension dimension)
    {
        Dimension = dimension;
    }

    public void Set(Dimension dimension)
    {
        Dimension = dimension;
        DrawInvul = false;
    }
}

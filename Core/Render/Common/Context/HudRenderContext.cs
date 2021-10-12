using System;
using Helion.Geometry;

namespace Helion.Render.Common.Context;

public class HudRenderContext
{
    public readonly Dimension Dimension;

    [Obsolete("Only here as a hack for the old renderer")]
    public bool DrawInvul { get; set; }

    public HudRenderContext(Dimension dimension)
    {
        Dimension = dimension;
    }
}

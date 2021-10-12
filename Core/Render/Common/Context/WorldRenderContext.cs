using System;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.World;

namespace Helion.Render.Common.Context;

public class WorldRenderContext
{
    public readonly Camera Camera;
    public readonly float InterpolationFrac;
    public Dimension Viewport { get; } = (640, 480);

    [Obsolete("This is a hack for the old legacy renderer")]
    public bool DrawAutomap;

    [Obsolete("This is a hack for the old legacy renderer")]
    public Vec2I AutomapOffset { get; set; } = (0, 0);

    [Obsolete("This is a hack for the old legacy renderer")]
    public double AutomapScale { get; set; } = 1.0;

    [Obsolete("This is a hack for the old legacy renderer")]

    public WorldRenderContext(Camera camera, float interpolationFrac)
    {
        Camera = camera;
        InterpolationFrac = interpolationFrac;
    }
}

using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.World;

namespace Helion.Render.Common.Context;

public class WorldRenderContext
{
    public readonly Camera Camera;
    public float InterpolationFrac;
    public Dimension Viewport { get; } = (640, 480);
    public bool DrawAutomap;
    public Vec2I AutomapOffset { get; set; } = (0, 0);
    public double AutomapScale { get; set; } = 1.0;

    public WorldRenderContext(Camera camera, float interpolationFrac)
    {
        Camera = camera;
        InterpolationFrac = interpolationFrac;
    }

    public void Set(float interpolationFrac, bool drawAutomap, Vec2I automapOffset, double automapScale)
    {
        InterpolationFrac = interpolationFrac;
        DrawAutomap = drawAutomap;
        AutomapOffset = automapOffset;
        AutomapScale = automapScale;
    }
}

using Helion.Geometry;

namespace Helion.Render.Common.World
{
    public class WorldRenderContext
    {
        public readonly Camera Camera;
        public readonly float InterpolationFrac;
        public Dimension Viewport { get; internal set; } = (640, 480);

        public WorldRenderContext(Camera camera, float interpolationFrac)
        {
            Camera = camera;
            InterpolationFrac = interpolationFrac;
        }
    }
}

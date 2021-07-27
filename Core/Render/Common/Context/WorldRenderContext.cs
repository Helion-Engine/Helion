using System;
using Helion.Geometry;
using Helion.Render.Common.World;

namespace Helion.Render.Common.Context
{
    public class WorldRenderContext
    {
        public readonly Camera Camera;
        public readonly float InterpolationFrac;
        public Dimension Viewport { get; internal set; } = (640, 480);

        [Obsolete("This is a hack for the old legacy renderer")]
        public bool DrawAutomap;

        public WorldRenderContext(Camera camera, float interpolationFrac)
        {
            Camera = camera;
            InterpolationFrac = interpolationFrac;
        }
    }
}

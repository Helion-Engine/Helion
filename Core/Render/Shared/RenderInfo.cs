using System;
using System.Drawing;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Shared
{
    /// <summary>
    /// A simple container for render-related information.
    /// </summary>
    //[Obsolete("Use WorldRenderInfo instead.")]
    public class RenderInfo
    {
        public readonly Camera Camera;
        public readonly CameraInfo CameraInfo;
        public readonly float TickFraction;
        public readonly Rectangle Viewport;

        public RenderInfo(Camera camera, float tickFraction, Rectangle viewport)
        {
            Precondition(tickFraction >= 0.0 && tickFraction <= 1.0, "Tick fraction should be in the unit range");

            Camera = camera;
            CameraInfo = camera.Interpolated(tickFraction, true);
            TickFraction = tickFraction;
            Viewport = viewport;
        }
    }
}

using System.Drawing;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Shared
{
    /// <summary>
    /// A simple container for render-related information.
    /// </summary>
    public class RenderInfo
    {
        public readonly Camera Camera;
        public readonly float TickFraction;
        public readonly Rectangle Viewport;

        public RenderInfo(Camera camera, float tickFraction, Rectangle viewport)
        {
            Precondition(tickFraction >= 0.0 && tickFraction <= 1.0, "Tick fraction should be in the unit range");

            Camera = camera;
            TickFraction = tickFraction;
            Viewport = viewport;
        }
    }
}

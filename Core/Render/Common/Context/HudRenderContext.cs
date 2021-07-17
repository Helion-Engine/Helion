using Helion.Geometry;

namespace Helion.Render.Common.Context
{
    public class HudRenderContext
    {
        public readonly Dimension Dimension;
        public bool DrawAutomap { get; set; }

        public HudRenderContext(Dimension dimension)
        {
            Dimension = dimension;
        }
    }
}

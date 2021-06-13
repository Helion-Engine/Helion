using Helion.Geometry;
using Helion.Render.Common.Enums;

namespace Helion.Render.Common
{
    /// <summary>
    /// Information for a virtual resolution when rendering.
    /// </summary>
    public readonly struct VirtualResolutionInfo
    {
        public readonly Dimension Dimension;
        public readonly ResolutionScale Scale;

        public VirtualResolutionInfo(Dimension dimension, ResolutionScale scale)
        {
            Dimension = dimension;
            Scale = scale;
        }
    }
}

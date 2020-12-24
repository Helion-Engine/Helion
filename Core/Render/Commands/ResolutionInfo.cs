using Helion.Util.Geometry;

namespace Helion.Render.Commands
{
    /// <summary>
    /// Information when handling resolution.
    /// </summary>
    public record ResolutionInfo
    {
        /// <summary>
        /// The virtual dimension that should be used when drawing.
        /// </summary>
        public Dimension VirtualDimensions;

        /// <summary>
        /// If true, this will stretch the image. This is the same as rendering
        /// in 4:3 and then stretching it to fit some larger resolution.
        /// </summary>
        public bool StretchIfWidescreen;

        /// <summary>
        /// If the screen is widescreen (not 4:3) then this decides whether the
        /// origin drawing location (as in the top left) is at a 4:3 ratio, or
        /// at the actual top left. Suppose the window is 900x600. Then if this
        /// is true, the origin would be at (50, 0) since 800x600 is the 4:3
        /// and thus there is 100 pixels of stretching (so, 50 on the left and
        /// 50 pixels on the right, means the offset from the left will be 50).
        /// If this is false, then the origin will always be (0, 0).
        /// </summary>
        public bool CenterIfWidescreen;
    }
}

using Helion.Util.Geometry;

namespace Helion.Render.Commands
{
    /// <summary>
    /// Information when handling resolution.
    /// </summary>
    public struct ResolutionInfo
    {
        /// <summary>
        /// The virtual dimension that should be used when drawing.
        /// </summary>
        public Dimension VirtualDimensions;

        /// <summary>
        /// How to scale the image to the virtual resolution. This has no
        /// effect if the virtual dimension is the same scaling as the native
        /// resolution (as in 16:10 window with a 16:10 virtual dimension means
        /// this will have no effect).
        /// </summary>
        public ResolutionScale Scale;

        /// <summary>
        /// The aspect ratio to use. Note that this is the aspect ratio used
        /// and not the aspect ratio of VirtualDimensions
        public float AspectRatio;
    }
}

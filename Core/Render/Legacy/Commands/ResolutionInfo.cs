using Helion.Geometry;

namespace Helion.Render.Legacy.Commands
{
    /// <summary>
    /// Information when handling resolution.
    /// </summary>
    public readonly struct ResolutionInfo
    {
        /// <summary>
        /// The virtual dimension that should be used when drawing.
        /// </summary>
        public readonly Dimension VirtualDimensions;

        /// <summary>
        /// How to scale the image to the virtual resolution. This has no
        /// effect if the virtual dimension is the same scaling as the native
        /// resolution (as in 16:10 window with a 16:10 virtual dimension means
        /// this will have no effect).
        /// </summary>
        public readonly ResolutionScale Scale;

        /// <summary>
        /// The aspect ratio to use. Note that this is the aspect ratio used
        /// and not the aspect ratio of VirtualDimensions
        /// </summary>
        public readonly float AspectRatio;

        public ResolutionInfo(Dimension virtualDimensions, ResolutionScale scale, float aspectRatio)
        {
            VirtualDimensions = virtualDimensions;
            Scale = scale;
            AspectRatio = aspectRatio;
        }
    }
}

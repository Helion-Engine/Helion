using Helion.Util.Geometry;

namespace Helion.Bsp.Geometry
{
    /// <summary>
    /// A simple wrapper for a line that has sector information.
    /// </summary>
    /// <remarks>
    /// This is intended to be an indexed element in an array/list. It holds
    /// the minimal amount of information that is required for any users to
    /// get the required sector information from it.
    /// </remarks>
    public struct SectorLine
    {
        public const int NoLineToSectorId = BspSegment.NoSectorId;

        /// <summary>
        /// The delta X and Y values for the line segment this represents.
        /// </summary>
        public readonly Vec2D Delta;

        /// <summary>
        /// The front sector ID.
        /// </summary>
        public readonly int FrontSectorId;

        /// <summary>
        /// The back sector ID, if none exists it equals NoLineToSectorId.
        /// </summary>
        public readonly int BackSectorId;

        /// <summary>
        /// True if it's a one sided line segment, false if not.
        /// </summary>
        public bool OneSided => BackSectorId == NoLineToSectorId;

        /// <summary>
        /// True if it's a two sided line segment, false if not.
        /// </summary>
        public bool TwoSided => !OneSided;

        public SectorLine(Vec2D delta, int frontSectorId, int backSectorId)
        {
            Delta = delta;
            FrontSectorId = frontSectorId;
            BackSectorId = backSectorId;
        }
    }
}

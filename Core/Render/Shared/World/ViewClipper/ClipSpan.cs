using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Shared.World.ViewClipper
{
    /// <summary>
    /// Represents a range of start and end diamond angles.
    /// </summary>
    public struct ClipSpan
    {
        /// <summary>
        /// The starting angle.
        /// </summary>
        public readonly uint StartAngle;
        
        /// <summary>
        /// The ending angle.
        /// </summary>
        public readonly uint EndAngle;

        /// <summary>
        /// Creates a new span of angles.
        /// </summary>
        /// <param name="startAngle">The starting angle, must be less than or
        /// equal to the end angle.</param>
        /// <param name="endAngle">The ending angle, must be less than or equal
        /// to the start angle.</param>
        public ClipSpan(uint startAngle, uint endAngle)
        {
            Precondition(startAngle < endAngle, "Cannot have the end angle be less than the start angle");
            
            StartAngle = startAngle;
            EndAngle = endAngle;
        }

        /// <summary>
        /// Checks if the angle provided is inclusively contained in the range.
        /// </summary>
        /// <param name="angle">The angle to check.</param>
        /// <returns>True if it is, false if not.</returns>
        public bool Contains(uint angle) => angle >= StartAngle && angle <= EndAngle;

        /// <summary>
        /// Checks if the range formed by the first and second angle are in the
        /// clip span. The order of the arguments does not matter, the second
        /// argument can be smaller than the first.
        /// </summary>
        /// <param name="firstAngle">The first angle.</param>
        /// <param name="secondAngle">The second angle.</param>
        /// <returns>True if both are in this span, false otherwise.</returns>
        public bool Contains(uint firstAngle, uint secondAngle) => Contains(firstAngle) && Contains(secondAngle);
    }
}
using Helion.Bsp.Geometry;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;

namespace Helion.Bsp.States.Convex
{
    /// <summary>
    /// A single point which contains information on how the segment was
    /// traversed.
    /// </summary>
    public class ConvexTraversalPoint
    {
        /// <summary>
        /// The segment for this traversal.
        /// </summary>
        public readonly BspSegment Segment;

        /// <summary>
        /// The endpoint to which we arrived at the segment first when doing
        /// traversal.
        /// </summary>
        /// <remarks>
        /// Note that the endpoint indicates which endpoint of the segment we had
        /// arrived at first. For example, if Endpoint == End, that means we went
        /// through the segment from End -> Start.
        /// </remarks>
        public readonly Endpoint Endpoint;
        
        /// <summary>
        /// Gets the point to which we arrived at this segment first.
        /// </summary>
        /// <returns>The point for the endpoint of the segment.</returns>
        public readonly Vec2D Vertex;

        /// <summary>
        /// Creates a new traversal point.
        /// </summary>
        /// <param name="segment">The segment for the point we are referencing.
        /// </param>
        /// <param name="endpoint">The endpoint on the segment we are
        /// referencing.</param>
        public ConvexTraversalPoint(BspSegment segment, Endpoint endpoint)
        {
            Segment = segment;
            Endpoint = endpoint;
            Vertex = Segment[Endpoint];
        }
    }
}
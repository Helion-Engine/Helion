using Helion.Bsp.Geometry;
using Helion.Bsp.States.Convex;
using Helion.Util.Geometry.Segments.Enums;

namespace Helion.Bsp.Impl.Debuggable.Convex
{
    /// <summary>
    /// A tracker of convex state information.
    /// </summary>
    public class ConvexStates
    {
        /// <summary>
        /// The current state.
        /// </summary>
        public ConvexState State = ConvexState.Loaded;
        
        /// <summary>
        /// A traversal of the segments we've done thus far.
        /// </summary>
        public ConvexTraversal ConvexTraversal = new ConvexTraversal();
        
        /// <summary>
        /// The starting segment we chose.
        /// </summary>
        public BspSegment? StartSegment;
        
        /// <summary>
        /// The current segment we're evaluating.
        /// </summary>
        public BspSegment? CurrentSegment;
        
        /// <summary>
        /// The endpoint of the current segment.
        /// </summary>
        public Endpoint CurrentEndpoint = Endpoint.Start;
        
        /// <summary>
        /// What rotation we've seen with all of our lines thus far.
        /// </summary>
        public Rotation Rotation = Rotation.On;
        
        /// <summary>
        /// How many segments we've visited in our traversal.
        /// </summary>
        public int SegsVisited = 0;
        
        /// <summary>
        /// The total number of segments we should be visiting.
        /// </summary>
        public int TotalSegs = 0;
    }
}
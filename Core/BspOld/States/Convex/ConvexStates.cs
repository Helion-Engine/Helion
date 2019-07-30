using Helion.BspOld.Geometry;
using Helion.Util.Geometry;

namespace Helion.BspOld.States.Convex
{
    /// <summary>
    /// An enumeration for the state of the convex checker.
    /// </summary>
    public enum ConvexState
    {
        Loaded,
        Traversing,
        FinishedIsDegenerate,
        FinishedIsConvex,
        FinishedIsSplittable,
    }

    /// <summary>
    /// A tracker of convex state information.
    /// </summary>
    public class ConvexStates
    {
        public ConvexState State = ConvexState.Loaded;
        public ConvexTraversal ConvexTraversal = new ConvexTraversal();
        public BspSegment? StartSegment;
        public BspSegment? CurrentSegment;
        public Endpoint CurrentEndpoint = Endpoint.Start;
        public Rotation Rotation = Rotation.On;
        public int SegsVisited = 0;
        public int TotalSegs = 0;
    }
}

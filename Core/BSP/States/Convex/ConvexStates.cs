using Helion.BSP.Geometry;
using Helion.Util.Geometry;

namespace Helion.BSP.States.Convex
{
    public enum ConvexState
    {
        Loaded,
        Traversing,
        FinishedIsDegenerate,
        FinishedIsConvex,
        FinishedIsSplittable,
    }

    public class ConvexStates
    {
        public ConvexState State = ConvexState.Loaded;
        public VertexIndex StartIndex = new VertexIndex(0);
        public VertexIndex CurrentIndex = new VertexIndex(0);
        public SegmentIndex StartSegIndex = new SegmentIndex(0);
        public SegmentIndex SurrentSegIndex = new SegmentIndex(0);
        public Rotation Rotation = Rotation.On;
        public int SegsVisited = 0;
        public int TotalSegs = 0;
        public Endpoint CurrentEndpoint = Endpoint.Start;
        public ConvexTraversal ConvexTraversal;
    }
}

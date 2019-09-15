using System.Collections.Generic;
using System.Linq;
using Helion.Bsp.Geometry;
using Helion.Util.Geometry.Segments.Enums;

namespace Helion.Bsp.States.Convex
{
    public abstract class ConvexChecker
    {
        public ConvexStates States { get; private set; } = new ConvexStates();
        protected readonly VertexCountTracker VertexTracker = new VertexCountTracker();
        protected readonly Dictionary<int, List<ConvexTraversalPoint>> VertexMap = new Dictionary<int, List<ConvexTraversalPoint>>();

        public void Load(List<BspSegment> segments)
        {
            States = new ConvexStates();
            VertexMap.Clear();
            VertexTracker.Reset();
            
            foreach (BspSegment segment in segments)
            {
                AddSegmentEndpoint(segment, segment.StartIndex, Endpoint.Start);
                AddSegmentEndpoint(segment, segment.EndIndex, Endpoint.End);

                // If we know we're not convex, we're done (save computation).
                if (VertexTracker.HasTripleJunction)
                {
                    States.State = ConvexState.FinishedIsSplittable;
                    return;
                }
            }

            // If there's a dangling segment somewhere (a vertex that
            if (VertexTracker.HasTerminalLine)
            {
                States.State = ConvexState.FinishedIsSplittable;
                return;
            }
            
            States.State = ConvexState.Loaded;
            SetStateLoadedInfo(segments);
        }
        
        public abstract void Execute();
        
        private void AddSegmentEndpoint(BspSegment segment, int index, Endpoint endpoint)
        {
            ConvexTraversalPoint linePoint = new ConvexTraversalPoint(segment, endpoint);

            if (VertexMap.TryGetValue(index, out List<ConvexTraversalPoint>? linePoints))
            {
                linePoints.Add(linePoint);
                VertexTracker.Track(linePoints.Count);
            }
            else
            {
                List<ConvexTraversalPoint> newLinePoints = new List<ConvexTraversalPoint> { linePoint };
                VertexMap.Add(index, newLinePoints);
                VertexTracker.Track(newLinePoints.Count);
            }
        }
        
        private void SetStateLoadedInfo(List<BspSegment> segments)
        {
            // We're just picking a random vertex, and taking some random segment that
            // comes out of that vertex.
            int randomVertexIndex = VertexMap.Keys.First();
            ConvexTraversalPoint randomLinePoint = VertexMap[randomVertexIndex].First();
            BspSegment startSegment = randomLinePoint.Segment;

            States.CurrentEndpoint = randomLinePoint.Endpoint;
            States.StartSegment = startSegment;
            States.CurrentSegment = startSegment;
            States.TotalSegs = segments.Count;
        }
    }
}
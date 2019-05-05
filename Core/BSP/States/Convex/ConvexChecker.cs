using Helion.BSP.Geometry;
using Helion.Util;
using Helion.Util.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace Helion.BSP.States.Convex
{
    public enum VertexCounter
    {
        LessThanThree,
        Three
    }

    public class ConvexChecker
    {
        public ConvexStates States;
        private readonly VertexCountTracker vertexTracker = new VertexCountTracker();
        private readonly Dictionary<VertexIndex, List<LinePoint>> vertexMap = new Dictionary<VertexIndex, List<LinePoint>>();

        private void Clear()
        {
            States = new ConvexStates();
            vertexMap.Clear();
            vertexTracker.Reset();
        }

        private void SetLoadedStateInfo(List<BspSegment> segments)
        {
            // We're just picking a random vertex, and taking some random segment that
            // comes out of that vertex.
            VertexIndex randomKey = vertexMap.Keys.First();
            LinePoint randomLinePoint = vertexMap[randomKey].First();
            BspSegment startSegment = randomLinePoint.Segment;

            States.State = ConvexState.Loaded;
            States.CurrentEndpoint = randomLinePoint.Endpoint;
            States.StartSegment = startSegment;
            States.CurrentSegment = startSegment;
            States.TotalSegs = segments.Count;
        }

        private void AddSegmentEndpoint(BspSegment segment, VertexIndex index, Endpoint endpoint)
        {
            LinePoint linePoint = new LinePoint(segment, endpoint);

            if (vertexMap.TryGetValue(index, out List<LinePoint> linePoints)) 
            {
                linePoints.Add(linePoint);
                vertexTracker.Track(linePoints.Count);
            }
            else
            {
                List<LinePoint> newLinePoints = new List<LinePoint>() { linePoint };
                vertexMap.Add(index, newLinePoints);
                vertexTracker.Track(newLinePoints.Count);
            }
        }

        public virtual void Load(List<BspSegment> segments)
        {
            Clear();

            foreach (BspSegment segment in segments)
            {
                AddSegmentEndpoint(segment, segment.StartIndex, Endpoint.Start);
                AddSegmentEndpoint(segment, segment.EndIndex, Endpoint.End);

                // If we know we're not convex, we're done (save computation).
                if (vertexTracker.HasTripleJunction)
                {
                    States.State = ConvexState.FinishedIsSplittable;
                    return;
                }
            }

            // If there's a terminal segment somewhere, it's splittable.
            if (vertexTracker.HasTerminalLine)
            {
                States.State = ConvexState.FinishedIsSplittable;
                return;
            }

            SetLoadedStateInfo(segments);
        }

        public virtual void Execute()
        {
            Assert.Precondition(States.State == ConvexState.Loaded, "Need to load segments before executing convex traversal");

            BspSegment currentSeg = States.CurrentSegment;
            Vec2D firstVertex = currentSeg[States.CurrentEndpoint];
            Vec2D secondVertex = currentSeg.Opposite(States.CurrentEndpoint);
            VertexIndex oppositeIndex = currentSeg.OppositeIndex(States.CurrentEndpoint);

            // Since we know there are exactly two lines at each endpoint, check
            // the first one and see if it it's the current line are are on. If not
            // then it's our next seg, otherwise the second pair is our next seg.
            List<LinePoint> linesAtOppositeVertex = vertexMap[oppositeIndex];
            BspSegment nextSeg = linesAtOppositeVertex[0].Segment;
            if (ReferenceEquals(currentSeg, nextSeg))
                nextSeg = linesAtOppositeVertex[1].Segment;

            // We are on a current segment in the suspected convex polygon, now
            // we need to go to the next/third vertex and see what rotation
            // it has (if any, it could be two srtaight lines).
            Endpoint nextSegPivotEndpoint = nextSeg.EndpointFrom(oppositeIndex);
            Vec2D thirdVertex = nextSeg.Opposite(nextSegPivotEndpoint);

            Rotation rotation = BspSegment.Rotation(firstVertex, secondVertex, thirdVertex);
            if (rotation != Rotation.On)
            {
                if (States.Rotation == Rotation.On)
                {
                    States.Rotation = rotation;
                }
                else if (States.Rotation != rotation)
                {
                    States.State = ConvexState.FinishedIsSplittable;
                    return;
                }
            }

            States.ConvexTraversal.AddTraversal(currentSeg, States.CurrentEndpoint);
            States.CurrentSegment = nextSeg;
            States.CurrentEndpoint = nextSegPivotEndpoint;
            States.SegsVisited++;

            // Until we cycle back to the original segment, traversal is not done.
            if (!ReferenceEquals(currentSeg, States.StartSegment))
                return;

            // If we never rotated, it's a straight degenerate line (and not a single
            // convex polygon).
            if (States.Rotation == Rotation.On)
            {
                States.State = ConvexState.FinishedIsDegenerate;
                return;
            }

            bool isConvex = (States.SegsVisited == States.TotalSegs);
            States.State = (isConvex ? ConvexState.FinishedIsConvex : ConvexState.FinishedIsSplittable);
        }
    }
}

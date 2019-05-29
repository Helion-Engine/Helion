using Helion.BSP.Geometry;
using Helion.Util;
using Helion.Util.Geometry;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.BSP.States.Convex
{
    /// <summary>
    /// A readable enumeration for how many segments go inbound/outbound from
    /// some vertex.
    /// </summary>
    /// <remarks>
    /// This was made because using `bool` is less clear.
    /// </remarks>
    public enum VertexCounter
    {
        LessThanThree,
        Three
    }

    /// <summary>
    /// An instance that is responsible for convexity checking and remembering
    /// traversal ordering.
    /// </summary>
    public class ConvexChecker
    {
        /// <summary>
        /// The state of this convex checker.
        /// </summary>
        public ConvexStates States = new ConvexStates();

        private readonly VertexCountTracker vertexTracker = new VertexCountTracker();
        private readonly Dictionary<VertexIndex, List<LinePoint>> vertexMap = new Dictionary<VertexIndex, List<LinePoint>>();

        private bool ValidExecutionState() => States.State == ConvexState.Loaded || States.State == ConvexState.Traversing;

        private void SetLoadedStateInfo(IList<BspSegment> segments)
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

        private bool CompletedTraversalCycle(BspSegment segment)
        {
            return States.SegsVisited > 2 && ReferenceEquals(segment, States.StartSegment);
        }

        private void Reset()
        {
            States = new ConvexStates();
            vertexMap.Clear();
            vertexTracker.Reset();
        }

        /// <summary>
        /// Loads the segments for execution.
        /// </summary>
        /// <param name="segments">The segments to check for convexity.</param>
        public virtual void Load(IList<BspSegment> segments)
        {
            Reset();

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

        /// <summary>
        /// Executes a single step in convex checking.
        /// </summary>
        public virtual void Execute()
        {
            Precondition(ValidExecutionState(), $"Called convex checker execution in an invalid state");
            if (States.CurrentSegment == null)
                throw new HelionException("Invalid convext checker current segment state");

            States.State = ConvexState.Traversing;

            // We traverse around the suspected enclosed polygon with each of
            // the following rules:
            //    - NextSeg is the next attached segment in the cycle iteration
            //    - CurrentSeg and NextSeg share a 'pivot' vertex
            //    - CurrentEndpoint is the endpoint on the current segment that
            //      is not the pivot vertex
            //    - The third vertex is the vertex on the NextSeg that is not
            //      the pivot
            //
            // This is summed up by this image (assuming clockwise traversal):
            //
            //    Current endpoint             Pivot (aka: Opposite endpoint)
            //         (0)-----[CurrentSeg]-----(1)
            //                                   |
            //                                   |NextSeg
            //                                   |
            //                                  (2) Third vertex
            //
            // Each number is the vertex in the rotation order.

            BspSegment currentSeg = States.CurrentSegment;
            Vec2D firstVertex = currentSeg[States.CurrentEndpoint];
            Vec2D secondVertex = currentSeg.Opposite(States.CurrentEndpoint);
            VertexIndex pivotIndex = currentSeg.OppositeIndex(States.CurrentEndpoint);

            // Since we know there are exactly two lines at each endpoint, we
            // can select the next segment by whichever of the two is not the
            // current segment.
            List<LinePoint> linesAtPivot = vertexMap[pivotIndex];
            Invariant(linesAtPivot.Count == 2, "Expected two lines for every endpoint");

            BspSegment nextSeg = linesAtPivot[0].Segment;
            if (ReferenceEquals(currentSeg, nextSeg))
                nextSeg = linesAtPivot[1].Segment;

            Endpoint nextSegPivotEndpoint = nextSeg.EndpointFrom(pivotIndex);
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

            if (!CompletedTraversalCycle(nextSeg))
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

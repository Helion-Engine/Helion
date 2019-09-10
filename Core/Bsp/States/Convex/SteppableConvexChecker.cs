using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Bsp.Geometry;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.States.Convex
{
    /// <summary>
    /// An instance that is responsible for convexity checking and remembering
    /// traversal ordering.
    /// </summary>
    public class SteppableConvexChecker : IConvexChecker
    {
        /// <inheritdoc/>
        public ConvexStates States { get; private set; } = new ConvexStates();

        private readonly VertexCountTracker vertexTracker = new VertexCountTracker();
        private readonly Dictionary<int, List<ConvexTraversalPoint>> vertexMap = new Dictionary<int, List<ConvexTraversalPoint>>();
        
        /// <inheritdoc/>
        public void Load(List<BspSegment> segments)
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

            // If there's a dangling segment somewhere (a vertex that
            if (vertexTracker.HasTerminalLine)
            {
                States.State = ConvexState.FinishedIsSplittable;
                return;
            }

            SetLoadedStateInfo(segments);
        }

        /// <inheritdoc/>
        public void Execute()
        {
            Precondition(ValidExecutionState(), $"Called convex checker execution in an invalid state");
            if (States.CurrentSegment == null)
                throw new NullReferenceException("Should never have a null current segment (forgot to Load()?)");

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
            int pivotIndex = currentSeg.OppositeIndex(States.CurrentEndpoint);

            // Since we know there are exactly two lines at each endpoint, we
            // can select the next segment by whichever of the two is not the
            // current segment.
            List<ConvexTraversalPoint> linesAtPivot = vertexMap[pivotIndex];
            Invariant(linesAtPivot.Count == 2, "Expected two lines for every endpoint");

            BspSegment nextSeg = linesAtPivot[0].Segment;
            if (ReferenceEquals(currentSeg, nextSeg))
                nextSeg = linesAtPivot[1].Segment;

            Endpoint nextSegPivotEndpoint = nextSeg.EndpointFrom(pivotIndex);
            Vec2D thirdVertex = nextSeg.Opposite(nextSegPivotEndpoint);

            Rotation rotation = Seg2D.Rotation(firstVertex, secondVertex, thirdVertex);
            if (rotation != Rotation.On) 
            {
                if (States.Rotation == Rotation.On)
                    States.Rotation = rotation;
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

        private bool ValidExecutionState()
        {
            return States.State == ConvexState.Loaded || States.State == ConvexState.Traversing;
        }

        private void SetLoadedStateInfo(List<BspSegment> segments)
        {
            // We're just picking a random vertex, and taking some random segment that
            // comes out of that vertex.
            int randomVertexIndex = vertexMap.Keys.First();
            ConvexTraversalPoint randomLinePoint = vertexMap[randomVertexIndex].First();
            BspSegment startSegment = randomLinePoint.Segment;

            States.State = ConvexState.Loaded;
            States.CurrentEndpoint = randomLinePoint.Endpoint;
            States.StartSegment = startSegment;
            States.CurrentSegment = startSegment;
            States.TotalSegs = segments.Count;
        }

        private void AddSegmentEndpoint(BspSegment segment, int index, Endpoint endpoint)
        {
            ConvexTraversalPoint linePoint = new ConvexTraversalPoint(segment, endpoint);

            if (vertexMap.TryGetValue(index, out List<ConvexTraversalPoint>? linePoints))
            {
                linePoints.Add(linePoint);
                vertexTracker.Track(linePoints.Count);
            }
            else
            {
                List<ConvexTraversalPoint> newLinePoints = new List<ConvexTraversalPoint> { linePoint };
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
    }
}
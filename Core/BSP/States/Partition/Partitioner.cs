using Helion.BSP.Geometry;
using Helion.BSP.States.Miniseg;
using Helion.Util;
using Helion.Util.Geometry;
using NLog;
using System;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.BSP.States.Partition
{
    public class Partitioner
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public PartitionStates States = new PartitionStates();
        private readonly BspConfig config;
        private readonly VertexAllocator vertexAllocator;
        private readonly SegmentAllocator segmentAllocator;
        private readonly JunctionClassifier junctionClassifier;

        public Partitioner(BspConfig config, VertexAllocator vertexAllocator, SegmentAllocator segmentAllocator,
            JunctionClassifier junctionClassifier)
        {
            this.config = config;
            this.vertexAllocator = vertexAllocator;
            this.segmentAllocator = segmentAllocator;
            this.junctionClassifier = junctionClassifier;
        }

        private bool BetweenEndpoints(double tSplitter)
        {
            double epsilon = config.VertexWeldingEpsilon;
            return epsilon < tSplitter && tSplitter < 1.0 - epsilon;
        }

        private bool IntersectionTimeAtEndpoint(BspSegment segmentToSplit, double tSegment, out Endpoint endpoint)
        {
            Vec2D vertex = segmentToSplit.FromTime(tSegment);
            if (vertexAllocator.TryGetValue(vertex, out VertexIndex index))
            {
                endpoint = segmentToSplit.EndpointFrom(index);
                return true;
            }

            endpoint = default;
            return false;
        }

        private void HandleSplitter()
        {
            BspSegment splitter = States.Splitter;

            States.RightSegments.Add(splitter);
            if (splitter.TwoSided)
                States.LeftSegments.Add(splitter);
        }

        private void HandleCollinearSegment(BspSegment segment)
        {
            Precondition(!segment.IsMiniseg, "Should never be collinear to a miniseg");

            States.CollinearVertices.Add(segment.StartIndex);
            States.CollinearVertices.Add(segment.EndIndex);

            // We don't want the back side of a one-sided line (which doesn't
            // exist) to be visible to the side of the partition that shouldn't
            // care about it.
            if (segment.OneSided)
            {
                if (States.Splitter.SameDirection(segment))
                    States.RightSegments.Add(segment);
                else
                    States.LeftSegments.Add(segment);
            }
            else
            {
                States.RightSegments.Add(segment);
                States.LeftSegments.Add(segment);
            }
        }

        private void HandleParallelSegment(BspSegment segment)
        {
            BspSegment splitter = States.Splitter;

            if (splitter.Collinear(segment))
                HandleCollinearSegment(segment);
            else if (splitter.OnRight(segment.Start))
                States.RightSegments.Add(segment);
            else
                States.LeftSegments.Add(segment);
        }

        private void HandleEndpointIntersectionSplit(BspSegment segmentToSplit, Endpoint endpoint)
        {
            BspSegment splitter = States.Splitter;

            // We know that the endpoint argument is the vertex that was 
            // intersected by the splitter. This means the other endpoint is
            // on the left or the right side of the splitter, so we'll use 
            // that 'opposite' endpoint to check the side we should place the
            // segment on.
            Vec2D oppositeVertex = segmentToSplit.Opposite(endpoint);
            Rotation side = splitter.ToSide(oppositeVertex);
            Invariant(side != Rotation.On, "Ambiguous split, segment too small to determine splitter side");

            if (side == Rotation.Right)
                States.RightSegments.Add(segmentToSplit);
            else
                States.LeftSegments.Add(segmentToSplit);


            VertexIndex index = segmentToSplit.IndexFrom(endpoint);
            States.CollinearVertices.Add(index);
        }

        private void HandleNonEndpointIntersectionSplit(BspSegment segmentToSplit, double tSegment)
        {
            BspSegment splitter = States.Splitter;

            // Note for the future: If we ever change to using pointers or some
            // kind of reference that invalidates on a list resizing (like a
            // std::vector in C++), this will lead to undefined behavior since
            // it will add new segments.
            (BspSegment segA, BspSegment segB) = segmentAllocator.Split(segmentToSplit, tSegment);

            // Since we split the segment such that the line is:
            //
            //    Seg A  |  Seg B
            // [A]=======o=========[B]
            //           |
            //           | <-- Splitter
            //           X
            //
            // We can find which side of the splitter the segments are on by 
            // looking at where [A] or [B] ended up. Since the split causes us 
            // to know that the middle index is at the first segment's endpoint
            // (and the second segment start point), either or of these would 
            // tell us which side the segment is on... and when we know which 
            // one is on which side of the splitter, we automatically know the 
            // other segment from the split is on the other.
            Rotation side = splitter.ToSide(segA.Start);

            // It may be possible that a split occurs so close to the endpoint
            // (by some unfortunate map geometry) which screws up detecting 
            // which side we are on for the splitting. In such a case, we'll 
            // try checking both endpoints to see if this is the case and make
            // our decision accordingly.
            if (side == Rotation.On)
            {
                log.Warn("Very tight endpoint split perform with segment {0}", segmentToSplit);

                Rotation otherSide = splitter.ToSide(segmentToSplit.End);
                Invariant(otherSide != Rotation.On, "Segment being split is too small to detect which side of the splitter it's on");
                side = (otherSide == Rotation.Left ? Rotation.Right : Rotation.Left);
            }

            if (side == Rotation.Right)
            {
                States.RightSegments.Add(segA);
                States.LeftSegments.Add(segB);
            }
            else
            {
                States.LeftSegments.Add(segA);
                States.RightSegments.Add(segB);
            }

            // The reason we know it's the end index of segA is because the
            // splitter always has the first seg splitting based on the start.
            // A corollary is that this would be equal to segB.getStartIndex()
            // since they are equal.
            VertexIndex middleVertexIndex = segA.EndIndex;
            States.CollinearVertices.Add(middleVertexIndex);

            if (segmentToSplit.OneSided)
                junctionClassifier.AddSplitJunction(segA, segB);
        }

        private void HandleSplit(BspSegment segmentToSplit, double tSegment, double tSplitter)
        {
            Precondition(!BetweenEndpoints(tSplitter), "Should not have a segment crossing the splitter");

            if (IntersectionTimeAtEndpoint(segmentToSplit, tSegment, out Endpoint endpoint))
                HandleEndpointIntersectionSplit(segmentToSplit, endpoint);
            else
                HandleNonEndpointIntersectionSplit(segmentToSplit, tSegment);
        }

        private void HandleSegmentOnSide(BspSegment segmentToSplit)
        {
            if (States.Splitter.OnRight(segmentToSplit))
                States.RightSegments.Add(segmentToSplit);
            else
                States.LeftSegments.Add(segmentToSplit);
        }

        public void Load(BspSegment splitter, List<BspSegment> segments)
        {
            // OPTIMIZE: A better way of finding segs to split would be to
            // take the result when we did the checking for the best
            // splitter and remember having all the segments to be split for
            // the best segment. We throw away all that work, and then do an
            // O(n) check again on top of it.

            States = new PartitionStates();
            States.Splitter = splitter;
            States.SegsToSplit = segments;
        }

        public virtual void Execute()
        {
            Precondition(States.State != PartitionState.Finished, "Trying to partition when it's already completed");

            BspSegment splitter = States.Splitter;
            BspSegment segmentToSplit = States.SegsToSplit[States.CurrentSegToPartitionIndex];
            States.CurrentSegToPartitionIndex++;

            bool moreSplitsToDo = (States.CurrentSegToPartitionIndex >= States.SegsToSplit.Count);
            States.State = (moreSplitsToDo ? PartitionState.Working : PartitionState.Finished);

            if (ReferenceEquals(segmentToSplit, splitter))
            {
                HandleSplitter();
                return;
            }

            if (segmentToSplit.Parallel(splitter))
            {
                HandleParallelSegment(segmentToSplit);
                return;
            }

            bool splits = splitter.IntersectionAsLine(segmentToSplit, out double tSplitter, out double tSegment);
            Invariant(splits, "A non-parallel line should intersect");

            if (MathHelper.InNormalRange(tSegment))
                HandleSplit(segmentToSplit, tSegment, tSplitter);
            else
                HandleSegmentOnSide(segmentToSplit);
        }
    }
}

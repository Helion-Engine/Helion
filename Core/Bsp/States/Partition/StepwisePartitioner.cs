using System;
using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.States.Miniseg;
using Helion.Util;
using Helion.Util.Geometry;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.States.Partition
{
    /// <summary>
    /// A stepwise debuggable line partitioner.
    /// </summary>
    public class StepwisePartitioner : IPartitioner
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public PartitionStates States { get; private set; } = new PartitionStates();
        
        private readonly BspConfig m_config;
        private readonly SegmentAllocator m_segmentAllocator;
        private readonly JunctionClassifier m_junctionClassifier;

        /// <summary>
        /// Creates a partitioner tha allows steppable debugging.
        /// </summary>
        /// <param name="config">The config with partitioning info.</param>
        /// <param name="segmentAllocator">The segment allocator to create new
        /// BSP segments when splitting.</param>
        /// <param name="junctionClassifier">The junction classifier to update
        /// with new junctions.</param>
        public StepwisePartitioner(BspConfig config, SegmentAllocator segmentAllocator, JunctionClassifier junctionClassifier)
        {
            m_config = config;
            m_segmentAllocator = segmentAllocator;
            m_junctionClassifier = junctionClassifier;
        }
        
        /// <inheritdoc/>
        public void Load(BspSegment? splitter, List<BspSegment> segments)
        {
            if (splitter == null)
                throw new NullReferenceException("Invalid split calculator state (likely convex polygon that was classified as splittable wrongly)");
            Precondition(!splitter.IsMiniseg, "Cannot have a miniseg as a splitter");

            // OPTIMIZE: A better way of finding segments to split would be to
            // take the result when we did the checking for the best splitter
            // and remember having all the segments to be split for the best
            // segment. We throw away all that work, and then do an O(n) check
            // again on top of it.
            States = new PartitionStates();
            States.Splitter = splitter;
            States.SegsToSplit = segments;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            Precondition(States.State != PartitionState.Finished, "Trying to partition when it's already completed");
            if (States.Splitter == null)
                throw new NullReferenceException("Unexpected null splitter");

            BspSegment splitter = States.Splitter;
            BspSegment segToSplit = States.SegsToSplit[States.CurrentSegToPartitionIndex];
            States.CurrentSegToPartitionIndex++;

            bool doneAllSplitting = (States.CurrentSegToPartitionIndex >= States.SegsToSplit.Count);
            States.State = (doneAllSplitting ? PartitionState.Finished : PartitionState.Working);

            if (ReferenceEquals(segToSplit, splitter))
            {
                HandleSplitter(splitter);
                return;
            }

            if (segToSplit.Parallel(splitter))
            {
                HandleParallelSegment(splitter, segToSplit);
                return;
            }

            bool splits = splitter.IntersectionAsLine(segToSplit, out double splitterTime, out double segmentTime);
            Invariant(splits, "A non-parallel line should intersect");

            if (MathHelper.InNormalRange(segmentTime))
                HandleSplit(splitter, segToSplit, segmentTime, splitterTime);
            else
                HandleSegmentOnSide(splitter, segToSplit);
        }
        
        private bool BetweenEndpoints(double splitterTime)
        {
            double epsilon = m_config.VertexWeldingEpsilon;
            return epsilon < splitterTime && splitterTime < 1.0 - epsilon;
        }

        private bool IntersectionTimeAtEndpoint(BspSegment segmentToSplit, double segmentTime, out Endpoint endpoint)
        {
            // Note that we cannot attempt to look up the endpoint, because it
            // is possible that it may detect an unrelated vertex from a split
            // on another side which happens to be right where the split would
            // happen.
            //
            // For example, suppose a perpendicular split is going to occur.
            // The vertical splitter line would intersect it like so at the X:
            //
            //      o
            //      | Splitter
            //      o
            //
            // o----X----o
            //   Segment
            //
            // Now suppose that the bottom side of `Segment` was recursed on
            // first, and some splitter happened to already create a vertex at
            // the exact point that `Splitter` would hit `Segment` on the X. If
            // we were to check the vertex allocator for whether a vertex is 
            // near/at X or not, it will return true. However this split will 
            // clearly not be hitting an endpoint of `Segment`, so we'd get a 
            // 'true' result when it is definitely not intersecting at/near an
            // endpoint.
            //
            // Therefore our only solution is to do it by checking distances.
            Vec2D vertex = segmentToSplit.FromTime(segmentTime);

            if (vertex.Distance(segmentToSplit.Start) <= m_config.VertexWeldingEpsilon)
            {
                endpoint = Endpoint.Start;
                return true;
            }

            if (vertex.Distance(segmentToSplit.End) <= m_config.VertexWeldingEpsilon)
            {
                endpoint = Endpoint.End;
                return true;
            }

            endpoint = default;
            return false;
        }

        private void HandleSplitter(BspSegment splitter)
        {
            States.RightSegments.Add(splitter);
            if (splitter.TwoSided)
                States.LeftSegments.Add(splitter);
        }

        private void HandleCollinearSegment(BspSegment splitter, BspSegment segment)
        {
            Precondition(!segment.IsMiniseg, "Should never be collinear to a miniseg");

            States.CollinearVertices.Add(segment.StartIndex);
            States.CollinearVertices.Add(segment.EndIndex);

            // We don't want the back side of a one-sided line (which doesn't
            // exist) to be visible to the side of the partition that shouldn't
            // care about it.
            if (segment.OneSided)
            {
                if (splitter.SameDirection(segment))
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

        private void HandleParallelSegment(BspSegment splitter, BspSegment segment)
        {
            if (splitter.Collinear(segment))
                HandleCollinearSegment(splitter, segment);
            else if (splitter.OnRight(segment.Start))
                States.RightSegments.Add(segment);
            else
                States.LeftSegments.Add(segment);
        }

        private void HandleEndpointIntersectionSplit(BspSegment splitter, BspSegment segmentToSplit, Endpoint endpoint)
        {
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

            int index = segmentToSplit.IndexFrom(endpoint);
            States.CollinearVertices.Add(index);
        }

        private void HandleNonEndpointIntersectionSplit(BspSegment splitter, BspSegment segmentToSplit, double segmentTime)
        {
            // Note for the future: If we ever change to using pointers or some
            // kind of reference that invalidates on a list resizing (like a
            // std::vector in C++), this will lead to undefined behavior since
            // it will add new segments and cause them to become dangling.
            (BspSegment segA, BspSegment segB) = m_segmentAllocator.Split(segmentToSplit, segmentTime);

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
                Log.Warn("Very tight endpoint split perform with segment {0}", segmentToSplit);

                Rotation otherSide = splitter.ToSide(segmentToSplit.End);
                Invariant(otherSide != Rotation.On,
                    "Segment being split is too small to detect which side of the splitter it's on");
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
            int middleVertexIndex = segA.EndIndex;
            States.CollinearVertices.Add(middleVertexIndex);

            if (segmentToSplit.OneSided)
                m_junctionClassifier.AddSplitJunction(segA, segB);
        }

        private void HandleSplit(BspSegment splitter, BspSegment segmentToSplit, double segmentTime, double splitterTime)
        {
            Precondition(!BetweenEndpoints(splitterTime), "Should not have a segment crossing the splitter");

            if (IntersectionTimeAtEndpoint(segmentToSplit, segmentTime, out Endpoint endpoint))
                HandleEndpointIntersectionSplit(splitter, segmentToSplit, endpoint);
            else
                HandleNonEndpointIntersectionSplit(splitter, segmentToSplit, segmentTime);
        }

        private void HandleSegmentOnSide(BspSegment splitter, BspSegment segmentToSplit)
        {
            if (splitter.OnRight(segmentToSplit))
                States.RightSegments.Add(segmentToSplit);
            else
                States.LeftSegments.Add(segmentToSplit);
        }
    }
}
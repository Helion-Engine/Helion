using System;
using Helion.Bsp.Geometry;
using Helion.Bsp.States.Miniseg;
using Helion.Bsp.States.Partition;
using Helion.Util;
using Helion.Util.Assertion;

namespace Helion.Bsp.Impl.Debuggable
{
    public class DebuggablePartitioner : Partitioner
    {
        public DebuggablePartitioner(BspConfig config, SegmentAllocator segmentAllocator, JunctionClassifier junctionClassifier) : 
            base(config, segmentAllocator, junctionClassifier)
        {
        }

        public override void Execute()
        {
            Assert.Precondition(States.State != PartitionState.Finished, "Trying to partition when it's already completed");
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
            Assert.Invariant(splits, "A non-parallel line should intersect");

            // Note that it is possible for two segments that share endpoints
            // to calculate intersections to each other outside of the normal
            // range due (ex: D2M29 has one at approx t = 1.0000000000000002).
            // Because they share a point and aren't parallel, then the line
            // must be on one of the sides and isn't intersecting. This will
            // also avoid very small cuts as well since we definitely do not
            // want a cut happening at the pathological time seen above!
            if (splitter.SharesAnyEndpoints(segToSplit))
                HandleSegmentOnSide(splitter, segToSplit);
            else if (MathHelper.InNormalRange(segmentTime))
                HandleSplit(splitter, segToSplit, segmentTime, splitterTime);
            else
                HandleSegmentOnSide(splitter, segToSplit);
        }
    }
}
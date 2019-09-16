using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.States.Miniseg;
using Helion.Bsp.States.Partition;
using Helion.Util;

namespace Helion.Bsp.Impl.Optimized
{
    public class OptimizedPartitioner : Partitioner
    {
        public OptimizedPartitioner(BspConfig config, SegmentAllocator segmentAllocator, JunctionClassifier junctionClassifier) : 
            base(config, segmentAllocator, junctionClassifier)
        {
        }

        public override void Execute()
        {
            List<BspSegment> segsToSplit = States.SegsToSplit;
            BspSegment splitter = States.Splitter!;

            for (int i = 0; i < segsToSplit.Count; i++)
            {
                BspSegment segToSplit = segsToSplit[i];

                if (ReferenceEquals(segToSplit, splitter))
                {
                    HandleSplitter(splitter);
                    continue;
                }

                if (segToSplit.Parallel(splitter))
                {
                    HandleParallelSegment(splitter, segToSplit);
                    continue;
                }

                splitter.IntersectionAsLine(segToSplit, out double splitterTime, out double segmentTime);

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
}
using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.States.Split;

namespace Helion.Bsp.Impl.Optimized
{
    public class OptimizedSplitCalculator : SplitCalculator
    {
        public OptimizedSplitCalculator(BspConfig bspConfig) : base(bspConfig)
        {
        }

        public override void Execute()
        {
            List<BspSegment> segments = States.Segments;
            for (int i = 0; i < segments.Count; i++)
            {
                BspSegment splitter = segments[i];
                States.CurrentSegScore = CalculateScore(splitter);

                if (States.CurrentSegScore < States.BestSegScore)
                {
                    States.BestSegScore = States.CurrentSegScore;
                    States.BestSplitter = splitter;
                }
            }
        }
    }
}
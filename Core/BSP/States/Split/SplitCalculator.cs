using Helion.BSP.Geometry;
using Helion.Util;
using Helion.Util.Geometry;
using System;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.BSP.States.Split
{
    public class SplitCalculator
    {
        public SplitterStates States = new SplitterStates();
        private readonly BspConfig config;

        public SplitCalculator(BspConfig bspConfig) => config = bspConfig;

        private bool IsAxisAligned(BspSegment seg)
        {
            return seg.Direction != SegmentDirection.Vertical && seg.Direction != SegmentDirection.Horizontal;
        }

        private static double CalculateDistanceToNearestEndpoint(BspSegment segment, double tSegment)
        {
            Vec2D endpointVertex = (tSegment < 0.5 ? segment.Start : segment.End);
            Vec2D intersectionPoint = segment.FromTime(tSegment);
            return endpointVertex.Distance(intersectionPoint);
        }

        private static bool CheckEndpointEpsilon(double distance, double epsilon)
        {
            return MathHelper.AreEqual(0.0, distance, epsilon) || MathHelper.AreEqual(1.0, distance, epsilon);
        }

        private static bool NoSplitsAndLinesAllOnOneSide(int splitCount, int leftLines, int rightLines)
        {
            return splitCount == 0 && (leftLines == 0 || rightLines == 0);
        }

        private bool SplitOccursAtEndpoint(double distance) => CheckEndpointEpsilon(distance, config.VertexWeldingEpsilon);

        private bool IntersectionNearButNotAtEndpoint(double distance)
        {
            return CheckEndpointEpsilon(distance, config.PunishableEndpointDistance) &&
                   !CheckEndpointEpsilon(distance, config.VertexWeldingEpsilon);
        }

        private int CalculateScore(BspSegment splitter)
        {
            SplitWeights splitWeights = config.SplitWeights;
            int score = 0;

            if (IsAxisAligned(splitter))
                score += splitWeights.NotAxisAlignedScore;

            int splitCount = 0;
            int linesOnLeft = 0;
            int linesOnRight = 0;
            foreach (BspSegment segment in States.Segments)
            {
                if (ReferenceEquals(segment, splitter))
                    continue;

                if (splitter.Parallel(segment))
                {
                    if (!splitter.Collinear(segment))
                    {
                        if (splitter.OnRight(segment))
                            linesOnRight++;
                        else
                            linesOnLeft++;
                    }

                    continue;
                }

                bool intersects = segment.IntersectionAsLine(splitter, out double tSegment);
                Invariant(intersects, "Non-parallel lines for split calculations must intersect");

                double nearestDistance = CalculateDistanceToNearestEndpoint(segment, tSegment);
                if (MathHelper.InNormalRange(nearestDistance))
                {
                    if (SplitOccursAtEndpoint(nearestDistance))
                    {
                        if (splitter.OnRight(segment))
                            linesOnRight++;
                        else
                            linesOnLeft++;
                    }
                    else
                    {
                        splitCount++;
                    }
                }
                else if (splitter.OnRight(segment))
                    linesOnRight++;
                else
                    linesOnLeft++;

                // Even though we may have missed, we want to see how close it 
                // was to the splitter. If we pick something like t = 1.0001, 
                // that's very close to the line and will result in likely a 
                // very small seg being generated later on, so avoid this.
                if (IntersectionNearButNotAtEndpoint(nearestDistance))
                    score += splitWeights.NearEndpointSplitScore;
            }

            score += Math.Abs(linesOnLeft - linesOnRight) * splitWeights.LeftRightSplitImbalanceScore;

            // We do not want to select a splitter that is on the edge of the
            // map and has no intersections. The only cases for this are if we 
            // pick an edge on the outside hull of the map (which we do not 
            // want as a splitter ever unless it splits something) or a 
            // degenerate /convex hull which we don't care about as those 
            // shapes should never be passed into this function. Therefore, if
            // it's zero or more collinear traversals with no partitions, that 
            // is always the worst case.
            if (NoSplitsAndLinesAllOnOneSide(splitCount, linesOnLeft, linesOnRight))
                score = int.MaxValue;
            else
                score += splitCount * splitWeights.SplitScoreFactor;

            return score;
        }

        public void Load(List<BspSegment> segments)
        {
            Precondition(segments.Count > 0, "Cannot do BSP split calculations on an empty segment list");

            States = new SplitterStates();
            States.Segments = segments;
        }

        public virtual void Execute()
        {
            Precondition(States.State != SplitterState.Finished, "Trying to run a split checker when finished");
            Precondition(States.CurrentSegmentIndex < States.Segments.Count, "Out of range split calculator segment index");

            BspSegment splitter = States.Segments[States.CurrentSegmentIndex];
            States.CurrentSegScore = CalculateScore(splitter);

            if (States.CurrentSegScore < States.BestSegScore)
            {
                States.BestSegScore = States.CurrentSegScore;
                States.BestSplitter = splitter;
            }

            States.CurrentSegmentIndex++;

            bool hasSegmentsLeft = States.CurrentSegmentIndex < States.Segments.Count;
            States.State = (hasSegmentsLeft ? SplitterState.Working : SplitterState.Finished);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Maps.Components;
using Helion.Util;
using Helion.Util.Geometry.Segments.Enums;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.States.Split
{
    public class SplitCalculator
    {
        public const int NoNodeIndex = -1;
        
        public SplitterStates States { get; private set; } = new SplitterStates();
        private readonly BspConfig m_bspConfig;
        private readonly CollinearTracker m_collinearTracker;
        private readonly SplitDecisionHelper m_splitHelper;
        private bool m_checkedSplitHelper;
        private int m_nodeIndex = NoNodeIndex;
        private BitArray m_seenCollinear = new BitArray(0);

        public SplitCalculator(BspConfig bspConfig, CollinearTracker collinearTracker, SplitDecisionHelper splitHelper)
        {
            m_bspConfig = bspConfig;
            m_collinearTracker = collinearTracker;
            m_splitHelper = splitHelper;
        }
        
        public void Load(List<BspSegment> segments, int nodeIndex = NoNodeIndex)
        {
            Precondition(segments.Count > 0, "Cannot do BSP split calculations on an empty segment list");

            States = new SplitterStates { Segments = segments };
            m_seenCollinear = new BitArray(m_collinearTracker.Count);
            m_checkedSplitHelper = false;
        }

        public void Execute()
        {
            Precondition(States.State != SplitterState.Finished, "Trying to run a split checker when finished");
            Precondition(States.CurrentSegmentIndex < States.Segments.Count, "Out of range split calculator segment index");

            if (!m_checkedSplitHelper)
            {
                m_checkedSplitHelper = true;
                
                BspSegment? bestSplitter = m_splitHelper.FindSplitter(m_nodeIndex, States.Segments, out INode? node);
                if (bestSplitter != null)
                {
                    States.BestSplitter = bestSplitter;
                    States.BestSegScore = 0;
                    States.State = SplitterState.Finished;
                    return;
                }
            }

            SearchForBestSplitter();
        }

        private static double CalculateDistanceToNearestEndpoint(BspSegment segment, double tSegment)
        {
            Vec2D endpointVertex = (tSegment < 0.5 ? segment.Start : segment.End);
            Vec2D intersectionPoint = segment.FromTime(tSegment);
            return endpointVertex.Distance(intersectionPoint);
        }

        private static bool IsAxisAligned(BspSegment seg)
        {
            return seg.Direction == SegmentDirection.Vertical || seg.Direction == SegmentDirection.Horizontal;
        }

        private static bool CheckEndpointEpsilon(double distance, double epsilon)
        {
            return MathHelper.AreEqual(0.0, distance, epsilon) || MathHelper.AreEqual(1.0, distance, epsilon);
        }

        private static bool NoSplitsAndLinesAllOnOneSide(int splitCount, int leftLines, int rightLines)
        {
            return splitCount == 0 && (leftLines == 0 || rightLines == 0);
        }

        private static bool IsEffectivelyRightOfSplitter(BspSegment splitter, BspSegment segment)
        {
            Rotation startSide = splitter.ToSide(segment.Start);

            if (startSide == Rotation.On)
            {
                Rotation endSide = splitter.ToSide(segment.End);
                Precondition(endSide != Rotation.On, "Spllitter and segment are too close to determine sides");

                return endSide == Rotation.Right;
            }

            return startSide == Rotation.Right;
        }

        private void SearchForBestSplitter()
        {
            BspSegment splitter = States.Segments[States.CurrentSegmentIndex];

            if (!m_seenCollinear.Get(splitter.CollinearIndex))
            {
                States.CurrentSegScore = CalculateScore(splitter);

                if (States.CurrentSegScore < States.BestSegScore)
                {
                    Invariant(!splitter.IsMiniseg, "Should never be selecting a miniseg as a splitter");
                    States.BestSegScore = States.CurrentSegScore;
                    States.BestSplitter = splitter;
                }

                m_seenCollinear.Set(splitter.CollinearIndex, true);
            }

            States.CurrentSegmentIndex++;

            bool hasSegmentsLeft = States.CurrentSegmentIndex < States.Segments.Count;
            States.State = (hasSegmentsLeft ? SplitterState.Working : SplitterState.Finished);
        }

        private bool SplitOccursAtEndpoint(double distance)
        {
            return CheckEndpointEpsilon(distance, m_bspConfig.VertexWeldingEpsilon);
        }

        private bool IntersectionNearButNotAtEndpoint(double distance)
        {
            return CheckEndpointEpsilon(distance, m_bspConfig.PunishableEndpointDistance) &&
                   !CheckEndpointEpsilon(distance, m_bspConfig.VertexWeldingEpsilon);
        }

        private int CalculateScore(BspSegment splitter)
        {
            SplitWeights splitWeights = m_bspConfig.SplitWeights;
            int score = 0;

            if (!IsAxisAligned(splitter))
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
                        if (IsEffectivelyRightOfSplitter(splitter, segment))
                            linesOnRight++;
                        else
                            linesOnLeft++;
                    }

                    continue;
                }

                bool intersects = segment.IntersectionAsLine(splitter, out double tSegment);
                Invariant(intersects, "Non-parallel lines for split calculations must intersect");

                double nearestDistance = CalculateDistanceToNearestEndpoint(segment, tSegment);
                if (MathHelper.InNormalRange(tSegment))
                {
                    if (SplitOccursAtEndpoint(nearestDistance))
                    {
                        if (IsEffectivelyRightOfSplitter(splitter, segment))
                            linesOnRight++;
                        else
                            linesOnLeft++;
                    }
                    else
                    {
                        splitCount++;
                    }
                }
                else if (IsEffectivelyRightOfSplitter(splitter, segment))
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
            // degenerate/convex hull which we don't care about as those 
            // shapes should never be passed into this function. Therefore, if
            // it's zero or more collinear traversals with no partitions, that 
            // is always the worst case.
            if (NoSplitsAndLinesAllOnOneSide(splitCount, linesOnLeft, linesOnRight))
                score = int.MaxValue;
            else
                score += splitCount * splitWeights.SplitScoreFactor;

            return score;
        }
    }
}
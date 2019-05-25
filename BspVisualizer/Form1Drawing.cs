using Helion.BSP.Geometry;
using Helion.BSP.States;
using Helion.BSP.States.Convex;
using Helion.BSP.States.Partition;
using Helion.BSP.States.Split;
using Helion.Util.Geometry;
using System;
using System.Drawing;

namespace BspVisualizer
{
    public partial class Form1
    {
        /// <summary>
        /// Draws a segment on the canvas. This handles all of the transformations.
        /// </summary>
        /// <param name="g">The graphics to draw with.</param>
        /// <param name="pen">The pen to color the line with.</param>
        /// <param name="segment">The segment to draw.</param>
        private void DrawSegment(Graphics g, Pen pen, BspSegment segment)
        {
            Vec2I start = TransformPoint(segment.Start);
            Vec2I end = TransformPoint(segment.End);
            g.DrawLine(pen, start.X, start.Y, end.X, end.Y);
        }

        private void AddMajorStateInfo()
        {
            bottomLeftCornerDrawer.Add(
                Tuple.Create(Color.White, "State: "),
                Tuple.Create(Color.Cyan, bspBuilder.States.Current.ToString())
            );
        }

        private void PaintBackground(Graphics g, Rectangle windowBounds)
        {
            g.FillRectangle(blackBackgroundBrush, windowBounds);
        }

        private void PaintShadowLines(Graphics g)
        {
            shadowSegments.ForEach(seg => DrawSegment(g, seg.OneSided ? shadowOneSidedLinePen : shadowTwoSidedLinePen, seg));
        }

        private void PaintCurrentWorkItem(Graphics g)
        {
            BspWorkItem? workItem = bspBuilder.GetCurrentWorkItem();
            if (workItem == null)
                return;

            foreach (BspSegment seg in workItem.Segments)
                DrawSegment(g, seg.OneSided ? whitePen : grayPen, seg);
        }

        private void PaintTextInfo(Graphics g, Rectangle windowBounds)
        {
            bottomLeftCornerDrawer.DrawAndClear(g, windowBounds, defaultFont);
        }

        private void PaintCurrentState(Graphics g, Rectangle windowBounds)
        {
            switch (bspBuilder.States.Current)
            {
            case BuilderState.CheckingConvexity:
                DrawCheckingConvexity(g);
                break;
            case BuilderState.CreatingLeafNode:
                DrawCreatingLeafNode(g);
                break;
            case BuilderState.FindingSplitter:
                DrawFindingSplitter(g);
                break;
            case BuilderState.PartitioningSegments:
                DrawPartitioningSegments(g);
                break;
            case BuilderState.GeneratingMinisegs:
                DrawGeneratingMinisegs(g);
                break;
            case BuilderState.FinishingSplit:
                DrawFinishingSplit(g);
                break;
            case BuilderState.NotStarted:
            case BuilderState.Complete:
            default:
                break;
            }
        }

        private void DrawCheckingConvexity(Graphics g)
        {
            ConvexStates states = bspBuilder.ConvexChecker.States;

            if (states.StartSegment != null)
                DrawSegment(g, redPen, states.StartSegment);
            if (states.CurrentSegment != null)
                DrawSegment(g, cyanPen, states.CurrentSegment);

            bottomLeftCornerDrawer.Add(Tuple.Create(Color.White, "Substate: "), Tuple.Create(Color.Cyan, states.State.ToString()));
            bottomLeftCornerDrawer.Add(Tuple.Create(Color.White, $"Visited {states.SegsVisited} / {states.TotalSegs} segs"));
            bottomLeftCornerDrawer.Add(Tuple.Create(Color.White, "Rotation: "), Tuple.Create(Color.Aqua, states.Rotation.ToString()));
        }

        private void DrawCreatingLeafNode(Graphics g)
        {
            // TODO
        }

        private void DrawFindingSplitter(Graphics g)
        {
            SplitterStates states = bspBuilder.SplitCalculator.States;

            bottomLeftCornerDrawer.Add(Tuple.Create(Color.White, "Substate: "), Tuple.Create(Color.Cyan, states.State.ToString()));

            if (states.BestSplitter != null)
            {
                bottomLeftCornerDrawer.Add(Tuple.Create(Color.White, "Best segment score: "), Tuple.Create(Color.LightGreen, states.BestSegScore.ToString()));
                bottomLeftCornerDrawer.Add(Tuple.Create(Color.White, "Best segment: "), Tuple.Create(Color.Cyan, states.BestSplitter.ToString()));

                DrawSegment(g, redPen, states.BestSplitter);
            }

            if (states.State != SplitterState.Loaded && states.CurrentSegmentIndex < states.Segments.Count)
            {
                BspSegment currentSegment = states.Segments[states.CurrentSegmentIndex];

                bottomLeftCornerDrawer.Add(Tuple.Create(Color.White, "Current segment score: "), Tuple.Create(Color.LightGreen, states.CurrentSegScore.ToString()));
                bottomLeftCornerDrawer.Add(
                    Tuple.Create(Color.White, $"Current segment ({states.CurrentSegmentIndex} / {states.Segments.Count}): "),
                    Tuple.Create(Color.Cyan, currentSegment.ToString())
                );

                DrawSegment(g, cyanPen, currentSegment);
            }
        }

        private void DrawPartitioningSegments(Graphics g)
        {
            PartitionStates states = bspBuilder.Partitioner.States;

            bottomLeftCornerDrawer.Add(Tuple.Create(Color.White, "Substate: "), Tuple.Create(Color.Cyan, states.State.ToString()));

            foreach (BspSegment leftSegment in states.LeftSegments)
                DrawSegment(g, bluePen, leftSegment);
            foreach (BspSegment rightSegment in states.RightSegments)
                DrawSegment(g, redPen, rightSegment);

            if (states.Splitter != null)
                DrawSegment(g, yellowPen, states.Splitter);

            if (states.CurrentSegToPartitionIndex < states.SegsToSplit.Count)
            {
                BspSegment currentSegment = states.SegsToSplit[states.CurrentSegToPartitionIndex];
                DrawSegment(g, cyanPen, currentSegment);

                bottomLeftCornerDrawer.Add(Tuple.Create(Color.White, $"Segment {states.CurrentSegToPartitionIndex} / {states.SegsToSplit.Count}"));
            }
        }

        private void DrawGeneratingMinisegs(Graphics g)
        {
            // TODO
        }

        private void DrawFinishingSplit(Graphics g)
        {
            // TODO
        }
    }
}

using System;
using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Bsp.States;
using Helion.Bsp.States.Convex;
using Helion.Bsp.States.Miniseg;
using Helion.Bsp.States.Partition;
using Helion.Bsp.States.Split;
using Helion.BspOld.States;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;
using BuilderState = Helion.Bsp.States.BuilderState;

namespace Helion.Bsp.Builder.Stepwise
{
    /// <summary>
    /// A stepwise debuggable BSP builder that allows each step to be executed
    /// in an atomic way for easy debugging.
    /// </summary>
    public partial class StepwiseBspBuilder
    {
        /// <summary>
        /// Executes an atomic step forward, meaning it moves ahead by the most
        /// indivisible element that allows a debugging session to see every
        /// state change independently.
        /// </summary>
        public void Execute()
        {
            switch (State)
            {
            case BuilderState.NotStarted:
                LoadNextWorkItem();
                break;

            case BuilderState.CheckingConvexity:
                ExecuteConvexityCheck();
                break;

            case BuilderState.CreatingLeafNode:
                ExecuteLeafNodeCreation();
                break;

            case BuilderState.FindingSplitter:
                ExecuteSplitterFinding();
                break;

            case BuilderState.PartitioningSegments:
                ExecuteSegmentPartitioning();
                break;

            case BuilderState.GeneratingMinisegs:
                ExecuteMinisegGeneration();
                break;

            case BuilderState.FinishingSplit:
                ExecuteSplitFinalization();
                break;
            }
        }
        
        /// <inheritdoc/>
        protected override void LoadNextWorkItem()
        {
            Invariant(WorkItems.Count > 0, "Expected a root work item to be present");

            ConvexChecker.Load(WorkItems.Peek().Segments);
            State = BuilderState.CheckingConvexity;
        }

        /// <inheritdoc/>
        protected override void ExecuteConvexityCheck()
        {
            Invariant(WorkItems.Count < RecursiveOverflowAmount, "BSP recursive overflow detected");

            switch (ConvexChecker.States.State)
            {
            case ConvexState.Loaded:
            case ConvexState.Traversing:
                ConvexChecker.Execute();
                break;

            case ConvexState.FinishedIsDegenerate:
            case ConvexState.FinishedIsConvex:
                State = BuilderState.CreatingLeafNode;
                break;

            case ConvexState.FinishedIsSplittable:
                SplitCalculator.Load(WorkItems.Peek().Segments);
                State = BuilderState.FindingSplitter;
                break;
            }
        }

        /// <inheritdoc/>
        protected override void ExecuteLeafNodeCreation()
        {
            ConvexState convexState = ConvexChecker.States.State;
            Invariant(convexState == ConvexState.FinishedIsDegenerate || convexState == ConvexState.FinishedIsConvex, "Unexpected BSP leaf building state");

            if (convexState == ConvexState.FinishedIsConvex)
                AddConvexTraversalToTopNode();

            WorkItems.Pop();

            if (WorkItems.Empty())
            {
                Root.StripDegenerateNodes();
                State = BuilderState.Complete;
            }
            else
                LoadNextWorkItem();
        }

        /// <inheritdoc/>
        protected override void ExecuteSplitterFinding()
        {
            switch (SplitCalculator.States.State)
            {
            case SplitterState.Loaded:
            case SplitterState.Working:
                SplitCalculator.Execute();
                break;

            case SplitterState.Finished:
                Partitioner.Load(SplitCalculator.States.BestSplitter, WorkItems.Peek().Segments);
                State = BuilderState.PartitioningSegments;
                break;
            }
        }

        /// <inheritdoc/>
        protected override void ExecuteSegmentPartitioning()
        {
            switch (Partitioner.States.State)
            {
            case PartitionState.Loaded:
            case PartitionState.Working:
                Partitioner.Execute();
                break;

            case PartitionState.Finished:
                if (Partitioner.States.Splitter == null)
                    throw new NullReferenceException("Unexpected null partition splitter");
                BspSegment? splitter = Partitioner.States.Splitter;
                MinisegCreator.Load(splitter, Partitioner.States.CollinearVertices);
                State = BuilderState.GeneratingMinisegs;
                break;
            }
        }

        /// <inheritdoc/>
        protected override void ExecuteMinisegGeneration()
        {
            switch (MinisegCreator.States.State)
            {
            case MinisegState.Loaded:
            case MinisegState.Working:
                MinisegCreator.Execute();
                break;

            case MinisegState.Finished:
                State = BuilderState.FinishingSplit;
                break;
            }
        }

        /// <inheritdoc/>
        protected override void ExecuteSplitFinalization()
        {
            WorkItem currentWorkItem = WorkItems.Pop();

            BspNode parentNode = currentWorkItem.Node;
            BspNode leftChild = new BspNode();
            BspNode rightChild = new BspNode();
            parentNode.SetChildren(leftChild, rightChild);
            parentNode.Splitter = SplitCalculator.States.BestSplitter;

            // We arbitrarily decide to build left first, so left is stacked after.
            List<BspSegment> rightSegs = Partitioner.States.RightSegments;
            List<BspSegment> leftSegs = Partitioner.States.LeftSegments;
            rightSegs.AddRange(MinisegCreator.States.Minisegs);
            leftSegs.AddRange(MinisegCreator.States.Minisegs);

            string path = currentWorkItem.BranchPath;
            WorkItems.Push(new WorkItem(rightChild, rightSegs, path + "R"));
            WorkItems.Push(new WorkItem(leftChild, leftSegs, path + "L"));

            LoadNextWorkItem();
        }
    }
}
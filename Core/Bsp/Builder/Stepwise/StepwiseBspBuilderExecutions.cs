using Helion.Bsp.States;
using Helion.Bsp.States.Convex;
using static Helion.Util.Assertion.Assert;

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
            // TODO
        }

        /// <inheritdoc/>
        protected override void ExecuteSplitterFinding()
        {
            // TODO
        }

        /// <inheritdoc/>
        protected override void ExecuteSegmentPartitioning()
        {
            // TODO
        }

        /// <inheritdoc/>
        protected override void ExecuteMinisegGeneration()
        {
            // TODO
        }

        /// <inheritdoc/>
        protected override void ExecuteSplitFinalization()
        {
            // TODO
        }
    }
}
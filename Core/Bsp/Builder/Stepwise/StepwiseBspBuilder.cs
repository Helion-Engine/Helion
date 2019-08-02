using Helion.Bsp.Node;
using Helion.Bsp.States;
using Helion.Bsp.States.Convex;
using Helion.Bsp.States.Miniseg;
using Helion.Bsp.States.Partition;
using Helion.Bsp.States.Split;
using Helion.Maps;
using Helion.Util.Extensions;

namespace Helion.Bsp.Builder.Stepwise
{
    /// <summary>
    /// A stepwise debuggable BSP builder that allows each step to be executed
    /// in an atomic way for easy debugging.
    /// </summary>
    public partial class StepwiseBspBuilder : BspBuilderBase
    {
        /// <summary>
        /// A counter which is used to make sure that we don't enter some 
        /// infinite loop due to any bugs. In a properly implemented builder
        /// this will never be reached.
        /// </summary>
        private const int RecursiveOverflowAmount = 10000;
        
        /// <summary>
        /// The current state of the builder.
        /// </summary>
        public BuilderState State { get; private set; } = BuilderState.NotStarted;
        
        /// <summary>
        /// True if building is done and the tree can be extracted/read, false
        /// if building still needs to be done.
        /// </summary>
        public bool Done => State == BuilderState.Complete;
        
        /// <summary>
        /// Gets the current work item.
        /// </summary>
        public WorkItem? CurrentWorkItem => WorkItems.TryPeek(out WorkItem result) ? result : null;
        
        /// <summary>
        /// Creates a stepwise BSP builder from a map with a default config.
        /// </summary>
        /// <param name="map">The map to build from.</param>
        public StepwiseBspBuilder(IMap map) : base(map)
        {
        }

        /// <summary>
        /// Creates a stepwise BSP builder from a map.
        /// </summary>
        /// <param name="map">The map to build from.</param>
        /// <param name="config">The configuration settings for various BSP
        /// activities.</param>
        public StepwiseBspBuilder(IMap map, BspConfig config) : base(map, config)
        {
        }
        
        /// <summary>
        /// Moves until either the current work item is the branch provided, or
        /// the building is done.
        /// </summary>
        /// <param name="branch">The branch path to go to.</param>
        public void ExecuteUntilBranch(string branch)
        {
            string upperBranch = branch.ToUpper();
            while (!Done)
            {
                WorkItem? item = CurrentWorkItem;
                if (item == null || item.BranchPath == upperBranch) 
                    break;
                ExecuteFullCycleStep();
            }
        }
        
        /// <summary>
        /// Steps through all major states until it reaches the convexity check
        /// or it completes.
        /// </summary>
        public void ExecuteFullCycleStep()
        {
            if (Done)
                return;

            do
                ExecuteMajorStep();
            while (State != BuilderState.CheckingConvexity && !Done);
        }

        /// <summary>
        /// Advances to the next major state.
        /// </summary>
        public void ExecuteMajorStep()
        {
            if (Done)
                return;

            BuilderState originalState = State;
            BuilderState currentState = State;
            while (originalState == currentState && !Done)
            {
                Execute();
                currentState = State;
            }
        }

        /// <inheritdoc/>
        public override BspNode? Build()
        {
            while (!Done)
                ExecuteFullCycleStep();
            
            Root.StripDegenerateNodes();
            return Root.IsDegenerate ? null : Root;
        }

        /// <inheritdoc/>
        protected override IConvexChecker CreateConvexChecker() => new SteppableConvexChecker();
        
        /// <inheritdoc/>
        protected override ISplitCalculator CreateSplitCalculator() => new SteppableSplitCalculator(BspConfig);

        /// <inheritdoc/>
        protected override IPartitioner CreatePartitioner() => new StepwisePartitioner(BspConfig, SegmentAllocator, JunctionClassifier);

        /// <inheritdoc/>
        protected override IMinisegCreator CreateMinisegCreator() => new SteppableMinisegCreator(VertexAllocator, SegmentAllocator, JunctionClassifier);
    }
}
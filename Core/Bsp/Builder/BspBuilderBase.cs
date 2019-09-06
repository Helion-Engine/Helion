using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Bsp.States;
using Helion.Bsp.States.Convex;
using Helion.Bsp.States.Miniseg;
using Helion.Bsp.States.Partition;
using Helion.Bsp.States.Split;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Builder
{
    /// <summary>
    /// The base class for all BSP builders.
    /// </summary>
    public abstract class BspBuilderBase : IBspBuilder
    {
        /// <summary>
        /// The allocator for all vertices in the map.
        /// </summary>
        public readonly VertexAllocator VertexAllocator;
        
        /// <summary>
        /// The segment allocator for all segments and splits in the map.
        /// </summary>
        public readonly SegmentAllocator SegmentAllocator;
        
        /// <summary>
        /// The object responsible for doing convexity checks.
        /// </summary>
        public readonly IConvexChecker ConvexChecker;

        /// <summary>
        /// The object responsible for calculating the best splits.
        /// </summary>
        public readonly ISplitCalculator SplitCalculator;

        /// <summary>
        /// The object responsible for partitioning the lines after we have
        /// found the best splitter.
        /// </summary>
        public readonly IPartitioner Partitioner;
        
        /// <summary>
        /// The object responsible for partitioning the lines after we have
        /// found the best splitter.
        /// </summary>
        public readonly IMinisegCreator MinisegCreator;
        
        /// <summary>
        /// Classifies junctions, which tell us whether we are inside the map
        /// or outside of it relative to some point (when making minisegs).
        /// </summary>
        public readonly JunctionClassifier JunctionClassifier = new JunctionClassifier();

        /// <summary>
        /// The pruner that removes bad segments that affect the BSP builder
        /// from running properly.
        /// </summary>
        public readonly SegmentChainPruner SegmentChainPruner = new SegmentChainPruner();
        
        /// <summary>
        /// The config to control various operations that we may want to vary
        /// to get better performance or results.
        /// </summary>
        protected readonly BspConfig BspConfig;

        /// <summary>
        /// A tracker of all collinear lines to reduce the number of checks we
        /// do when calculating the best splitter.
        /// </summary>
        protected readonly CollinearTracker CollinearTracker;

        /// <summary>
        /// The root of the tree.
        /// </summary>
        protected readonly BspNode Root = new BspNode();

        /// <summary>
        /// All the work items when building a tree.
        /// </summary>
        protected readonly Stack<WorkItem> WorkItems = new Stack<WorkItem>();

        /// <summary>
        /// Populates the BSP builder internals with a default config.
        /// </summary>
        /// <param name="map">The map to use the geometry from.</param>
        protected BspBuilderBase(IMap map) : this(map, new BspConfig())
        {
        }
        
        /// <summary>
        /// Populates the BSP builder internals.
        /// </summary>
        /// <param name="map">The map to use the geometry from.</param>
        /// <param name="config">The configuration settings for various BSP
        /// activities.</param>
        protected BspBuilderBase(IMap map, BspConfig config)
        {
            BspConfig = config;
            VertexAllocator = new VertexAllocator(config.VertexWeldingEpsilon);
            CollinearTracker = new CollinearTracker(config.VertexWeldingEpsilon);
            SegmentAllocator = new SegmentAllocator(VertexAllocator, CollinearTracker);
            ConvexChecker = CreateConvexChecker();
            SplitCalculator = CreateSplitCalculator();
            Partitioner = CreatePartitioner();
            MinisegCreator = CreateMinisegCreator();
            
            List<BspSegment> prunedSegments = ReadAndPopulateAllocatorsFrom(map);
            WorkItems.Push(new WorkItem(Root, prunedSegments));
        }

        /// <inheritdoc/>
        public abstract BspNode? Build();
        
        /// <summary>
        /// Takes the convex traversal that was done and adds it to the top BSP 
        /// node on the stack. This effectively creates the subsector.
        /// </summary>
        protected void AddConvexTraversalToTopNode()
        {
            Invariant(!WorkItems.Empty(), "Cannot add convex traversal to an empty work item stack");

            ConvexTraversal traversal = ConvexChecker.States.ConvexTraversal;
            Rotation rotation = ConvexChecker.States.Rotation;
            List<SubsectorEdge> edges = SubsectorEdge.FromClockwiseTraversal(traversal, rotation);
            
            WorkItems.Peek().Node.ClockwiseEdges = edges;
        }

        /// <summary>
        /// Forces the parent implementing classes to provide some instance of
        /// this interface.
        /// </summary>
        /// <returns>A convex checking object.</returns>
        protected abstract IConvexChecker CreateConvexChecker();

        /// <summary>
        /// Forces the parent implementing classes to provide some instance of
        /// this interface.
        /// </summary>
        /// <returns>A split calculator object.</returns>
        protected abstract ISplitCalculator CreateSplitCalculator();
        
        /// <summary>
        /// Forces the parent implementing classes to provide some instance of
        /// this interface.
        /// </summary>
        /// <returns>A partitioning object.</returns>
        protected abstract IPartitioner CreatePartitioner();
        
        /// <summary>
        /// Forces the parent implementing classes to provide some instance of
        /// this interface.
        /// </summary>
        /// <returns>A partitioning object.</returns>
        protected abstract IMinisegCreator CreateMinisegCreator();

        /// <summary>
        /// Loads the next work item for processing.
        /// </summary>
        protected abstract void LoadNextWorkItem();

        /// <summary>
        /// Performs the convexity checking.
        /// </summary>
        protected abstract void ExecuteConvexityCheck();

        /// <summary>
        /// Creates leaf nodes from our convex subsector.
        /// </summary>
        protected abstract void ExecuteLeafNodeCreation();

        /// <summary>
        /// Performs discovery of the best splitter.
        /// </summary>
        protected abstract void ExecuteSplitterFinding();

        /// <summary>
        /// Uses the best splitter found to partition the segments.
        /// </summary>
        protected abstract void ExecuteSegmentPartitioning();

        /// <summary>
        /// Generates minisegs along the splitter line.
        /// </summary>
        protected abstract void ExecuteMinisegGeneration();

        /// <summary>
        /// Finalizes all of the actions taken thus far, and prepares the next
        /// iteration.
        /// </summary>
        protected abstract void ExecuteSplitFinalization();
        
        private List<BspSegment> ReadAndPopulateAllocatorsFrom(IMap map)
        {
            List<BspSegment> segments = new List<BspSegment>();
            foreach (ILine line in map.GetLines())
            {
                int startIndex = VertexAllocator[line.GetStart().Position];
                int endIndex = VertexAllocator[line.GetEnd().Position];
                BspSegment segment = SegmentAllocator.GetOrCreate(startIndex, endIndex, line);
                segments.Add(segment);
            }
            
            // TODO: Extract both out of the following out
            
            List<BspSegment> prunedSegments = SegmentChainPruner.Prune(segments);
            
            // The junction classifier will not generate the junctions until we
            // notify that we're done adding, because adding one by one and
            // calculating on the fly would be pretty taxing and lead to O(n^2)
            // work and we'd rather have O(n).
            prunedSegments.ForEach(JunctionClassifier.Add);
            JunctionClassifier.NotifyDoneAdding();

            return prunedSegments;
        }
    }
}
using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Bsp.States;
using Helion.Bsp.States.Convex;
using Helion.Bsp.States.Miniseg;
using Helion.Bsp.States.Partition;
using Helion.Bsp.States.Split;
using Helion.Maps;
using Helion.Maps.Geometry.Lines;

namespace Helion.Bsp.Builder
{
    /// <summary>
    /// The base class for all BSP builders.
    /// </summary>
    public abstract class BspBuilderBase : IBspBuilder
    {
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
        /// The config to control various operations that we may want to vary
        /// to get better performance or results.
        /// </summary>
        protected readonly BspConfig BspConfig;
        
        /// <summary>
        /// The allocator for all vertices in the map.
        /// </summary>
        protected readonly VertexAllocator VertexAllocator;
        
        /// <summary>
        /// A tracker of all collinear lines to reduce the number of checks we
        /// do when calculating the best splitter.
        /// </summary>
        protected readonly CollinearTracker CollinearTracker;
        
        /// <summary>
        /// The segment allocator for all segments and splits in the map.
        /// </summary>
        protected readonly SegmentAllocator SegmentAllocator;

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
            
            PopulateAllocatorsFrom(map);
            
            WorkItems.Push(new WorkItem(Root, SegmentAllocator.ToList()));
        }

        /// <inheritdoc/>
        public abstract BspNode? Build();

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
        
        private void PopulateAllocatorsFrom(IMap map)
        {
            foreach (Line line in map.Lines)
            {
                int startIndex = VertexAllocator[line.StartVertex.Position];
                int endIndex = VertexAllocator[line.EndVertex.Position];
                BspSegment bspSegment = SegmentAllocator.GetOrCreate(startIndex, endIndex, line);
                JunctionClassifier.Add(bspSegment);
            }
            
            // The junction classifier will not generate the junctions until we
            // call this function, because adding one by one and calculating on
            // the fly would be pretty taxing and lead to O(n^2) calculations.
            JunctionClassifier.NotifyDoneAdding();
        }
    }
}
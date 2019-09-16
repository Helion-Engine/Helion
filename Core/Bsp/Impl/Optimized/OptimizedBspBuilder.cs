using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Bsp.States;
using Helion.Bsp.States.Convex;
using Helion.Maps;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Segments.Enums;

namespace Helion.Bsp.Impl.Optimized
{
    public class OptimizedBspBuilder : BspBuilder
    {
        private readonly OptimizedConvexChecker ConvexChecker;
        private readonly OptimizedSplitCalculator SplitCalculator;
        private readonly OptimizedPartitioner Partitioner;
        private readonly OptimizedMinisegCreator MinisegCreator;

        public OptimizedBspBuilder(IMap map) : this(new BspConfig(), map)
        {
        }
        
        public OptimizedBspBuilder(BspConfig config, IMap map) : base(config, map)
        {
            ConvexChecker = new OptimizedConvexChecker();
            SplitCalculator = new OptimizedSplitCalculator(config);
            Partitioner = new OptimizedPartitioner(config, SegmentAllocator, JunctionClassifier);
            MinisegCreator = new OptimizedMinisegCreator(VertexAllocator, SegmentAllocator, JunctionClassifier);
        }

        public override BspNode? Build()
        {
            bool hasDegenerateNode = false;
            
            while (!WorkItems.Empty())
            {
                WorkItem workItem = WorkItems.Pop();
                List<BspSegment> segments = workItem.Segments;
                
                ConvexChecker.Load(segments);
                
                if (ConvexChecker.States.State == ConvexState.Loaded)
                    ConvexChecker.Execute();
                
                switch (ConvexChecker.States.State)
                {
                case ConvexState.FinishedIsDegenerate:
                    hasDegenerateNode = true;
                    goto case ConvexState.FinishedIsConvex;
                case ConvexState.FinishedIsConvex:
                    HandleConvexNode(workItem, ConvexChecker.States.State);
                    continue;
                }
                
                SplitCalculator.Load(segments);
                SplitCalculator.Execute();

                BspSegment splitter = SplitCalculator.States.BestSplitter!;
                Partitioner.Load(splitter, segments);
                Partitioner.Execute();
                
                MinisegCreator.Load(splitter, Partitioner.States.CollinearVertices);
                MinisegCreator.Execute();

                FinishBspSplitting(workItem);
            }
            
            if (hasDegenerateNode)
                Root.StripDegenerateNodes();
            
            return Root;
        }

        private void HandleConvexNode(WorkItem workItem, ConvexState state)
        {
            if (state == ConvexState.FinishedIsConvex)
            {
                ConvexTraversal traversal = ConvexChecker.States.ConvexTraversal;
                Rotation rotation = ConvexChecker.States.Rotation;
                List<SubsectorEdge> edges = SubsectorEdge.FromClockwiseTraversal(traversal, rotation);
            
                workItem.Node.ClockwiseEdges = edges;
            }
            
            if (!WorkItems.Empty())
                ConvexChecker.Load(WorkItems.Peek().Segments);
        }

        private void FinishBspSplitting(WorkItem currentWorkItem)
        {
            BspNode parentNode = currentWorkItem.Node;
            BspNode leftChild = new BspNode();
            BspNode rightChild = new BspNode();
            parentNode.SetChildren(leftChild, rightChild);
            parentNode.Splitter = SplitCalculator.States.BestSplitter;

            List<BspSegment> rightSegs = Partitioner.States.RightSegments;
            List<BspSegment> leftSegs = Partitioner.States.LeftSegments;
            rightSegs.AddRange(MinisegCreator.States.Minisegs);
            leftSegs.AddRange(MinisegCreator.States.Minisegs);

            WorkItems.Push(new WorkItem(rightChild, rightSegs));
            WorkItems.Push(new WorkItem(leftChild, leftSegs));    
        }
    }
}
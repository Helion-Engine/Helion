using System.Collections.Generic;
using Helion.Bsp.Geometry;

namespace Helion.Bsp.States.Convex
{
    public abstract class ConvexChecker
    {
        protected readonly VertexCountTracker VertexTracker = new VertexCountTracker();
        protected readonly Dictionary<int, List<ConvexTraversalPoint>> VertexMap = new Dictionary<int, List<ConvexTraversalPoint>>();
        
        public abstract void Load(List<BspSegment> segments);
        
        public abstract void Execute();
    }
}
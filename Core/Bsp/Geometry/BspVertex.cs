using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Util.Geometry.Graphs;

namespace Helion.Bsp.Geometry
{
    public class BspVertex : IGraphVertex
    {
        public readonly Vec2D Position;
        public readonly int Index;
        public readonly List<BspSegment> Edges = new();

        public BspVertex(Vec2D position, int index)
        {
            Position = position;
            Index = index;
        }

        public IReadOnlyList<IGraphEdge> GetEdges() => Edges;

        public override string ToString() => $"{Position} (index = {Index}, edgeCount = {Edges.Count})";
    }
}
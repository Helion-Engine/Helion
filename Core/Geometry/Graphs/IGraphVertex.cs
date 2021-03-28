using System.Collections.Generic;

namespace Helion.Geometry.Graphs
{
    public interface IGraphVertex
    {
        IReadOnlyList<IGraphEdge> GetEdges();
    }
}
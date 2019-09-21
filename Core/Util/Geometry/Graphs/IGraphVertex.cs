using System.Collections.Generic;

namespace Helion.Util.Geometry.Graphs
{
    public interface IGraphVertex
    {
        IReadOnlyList<IGraphEdge> GetEdges();
    }
}
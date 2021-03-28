using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Util.Extensions;

namespace Helion.Geometry.Graphs
{
    public abstract class Graph<V, E> where V : IGraphVertex where E : IGraphEdge
    {
        public static void Traverse(V start, E edge, 
            Func<V, V, E, (GraphIterationStatus status, V nextVertex, E nextEdge)> func)
        {
            V prev = start;
            V current = (V)(edge.GetStart().Equals(prev) ? edge.GetEnd() : edge.GetStart());
            E currentEdge = edge;
            
            while (true)
            {
                (GraphIterationStatus status, V nextVertex, E nextEdge) = func(prev, current, currentEdge);

                if (status == GraphIterationStatus.Stop)
                    break;
                
                prev = current;
                current = nextVertex;
                currentEdge = nextEdge;
            }
        }
        
        public void Traverse(Func<V, V, E, (GraphIterationStatus status, V nextVertex, E nextEdge)> func)
        {
            if (GetEdges().Empty())
                return;
            
            E edge = GetEdges().First();
            V start = (V)edge.GetStart();
            Traverse(start, edge, func);
        }
        
        protected abstract IEnumerable<V> GetVertices();
        protected abstract IEnumerable<E> GetEdges();
    }
}
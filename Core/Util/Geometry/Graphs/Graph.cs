using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Util.Extensions;

namespace Helion.Util.Geometry.Graphs
{
    public abstract class Graph<V, E> where V : IGraphVertex where E : IGraphEdge
    {
        public abstract IEnumerable<V> Vertices { get; }
        public abstract IEnumerable<E> Edges { get; }

        public void Traverse(Func<V, V, E, (V nextVertex, E nextEdge)> func)
        {
            if (Edges.Empty())
                return;
            
            E edge = Edges.First();
            V start = (V)edge.GetStart();
            V end = (V)edge.GetEnd();
            Traverse(start, end, edge, func);
        }
        
        public void Traverse(V start, V end, E edge, Func<V, V, E, (V nextVertex, E nextEdge)> func)
        {
            V prev = start;
            V current = end;
            E currentEdge = edge;
            
            while (current != null && currentEdge != null)
            {
                // Note: This function can return null. We have to work around
                // nullable references not playing nicely with the types since
                // v? is equal to Nullable<V> which as of C# 8.0 does not want
                // to support assignment for generic types as nullable.
                (V nextVertex, E nextEdge) = func(prev, current, currentEdge);
                
                prev = current;
                current = nextVertex;
                currentEdge = nextEdge;
            }
        }
    }
}
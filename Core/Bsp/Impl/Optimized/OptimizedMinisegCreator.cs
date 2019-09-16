using Helion.Bsp.Geometry;
using Helion.Bsp.States.Miniseg;

namespace Helion.Bsp.Impl.Optimized
{
    public class OptimizedMinisegCreator : MinisegCreator
    {
        public OptimizedMinisegCreator(VertexAllocator vertexAllocator, SegmentAllocator segmentAllocator, JunctionClassifier junctionClassifier) : 
            base(vertexAllocator, segmentAllocator, junctionClassifier)
        {
        }

        public override void Execute()
        {
            int numVertices = States.Vertices.Count - 1;
            for (int i = 0; i < numVertices; i++)
            {
                VertexSplitterTime first = States.Vertices[i];
                VertexSplitterTime second = States.Vertices[i + 1];
                HandleMinisegGeneration(first, second);
            }
        }
    }
}
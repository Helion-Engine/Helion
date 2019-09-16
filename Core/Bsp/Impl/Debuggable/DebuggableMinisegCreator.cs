using Helion.Bsp.Geometry;
using Helion.Bsp.States.Miniseg;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Impl.Debuggable
{
    public class DebuggableMinisegCreator : MinisegCreator
    {
        public DebuggableMinisegCreator(VertexAllocator vertexAllocator, SegmentAllocator segmentAllocator, JunctionClassifier junctionClassifier) : 
            base(vertexAllocator, segmentAllocator, junctionClassifier)
        {
        }

        public override void Execute()
        {
            Precondition(States.State != MinisegState.Finished, "Trying to do miniseg generation when already finished");
            Precondition(States.CurrentVertexListIndex + 1 < States.Vertices.Count, "Overflow of vertex sliding window");

            VertexSplitterTime first = States.Vertices[States.CurrentVertexListIndex];
            VertexSplitterTime second = States.Vertices[States.CurrentVertexListIndex + 1];
            States.CurrentVertexListIndex++;

            HandleMinisegGeneration(first, second);

            bool isDone = (States.CurrentVertexListIndex + 1 >= States.Vertices.Count);
            States.State = (isDone ? MinisegState.Finished : MinisegState.Working);
        }
    }
}
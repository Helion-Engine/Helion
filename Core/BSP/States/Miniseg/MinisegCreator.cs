using Helion.BSP.Geometry;
using Helion.Util.Geometry;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.BSP.States.Miniseg
{
    public class MinisegCreator
    {
        public MinisegStates States = new MinisegStates();
        private readonly JunctionClassifier junctionClassifier;
        private readonly VertexAllocator vertexAllocator;
        private readonly SegmentAllocator segmentAllocator;

        public MinisegCreator(VertexAllocator vertexAllocator, SegmentAllocator segmentAllocator, 
            JunctionClassifier junctionClassifier)
        {
            this.vertexAllocator = vertexAllocator;
            this.segmentAllocator = segmentAllocator;
            this.junctionClassifier = junctionClassifier;
        }

        private void HandleMinisegGeneration(VertexSplitterTime first, VertexSplitterTime second)
        {
            States.VoidStatus = VoidStatus.NotInVoid;

            // If a segment exists for the vertices then we're walking along a
            // segment that was collinear with the splitter, so we don't need a
            // miniseg.
            if (segmentAllocator.ContainsSegment(first.Index, second.Index))
                return;

            Vec2D secondVertex = vertexAllocator[second.Index];
            if (junctionClassifier.CheckCrossingVoid(first.Index, secondVertex))
                States.VoidStatus = VoidStatus.InVoid;
            else
            {
                BspSegment miniseg = segmentAllocator.GetOrCreate(first.Index, second.Index);
                States.Minisegs.Add(miniseg);
            }
        }

        public void Load(BspSegment splitter, HashSet<VertexIndex> collinearVertices)
        {
            Precondition(collinearVertices.Count >= 2, "Requires two or more vertices for miniseg generation (the splitter should have contributed two)");

            States = new MinisegStates();

            foreach (VertexIndex vertexIndex in collinearVertices)
            {
                double tSplitter = splitter.ToTime(vertexAllocator[vertexIndex]);
                States.Vertices.Add(new VertexSplitterTime(vertexIndex, tSplitter));
            }

            States.Vertices.Sort();
        }

        public void Execute()
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

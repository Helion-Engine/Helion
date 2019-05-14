using Helion.BSP.Geometry;
using Helion.Util.Geometry;
using NLog;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.BSP.States.Miniseg
{
    public class MinisegCreator
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public MinisegStates States = new MinisegStates();
        public JunctionClassifier JunctionClassifier = new JunctionClassifier();
        private readonly VertexAllocator vertexAllocator;
        private readonly SegmentAllocator segmentAllocator;

        public MinisegCreator(VertexAllocator vertexAllocator, SegmentAllocator segmentAllocator)
        {
            this.vertexAllocator = vertexAllocator;
            this.segmentAllocator = segmentAllocator;
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
            if (JunctionClassifier.CheckCrossingVoid(first.Index, secondVertex))
            {
                BspSegment miniseg = segmentAllocator.GetOrCreate(first.Index, second.Index);
                States.Minisegs.Add(miniseg);
                States.VoidStatus = VoidStatus.InVoid;
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
            Precondition(States.CurrentListIndex + 1 < States.Vertices.Count, "Overflow of vertex sliding window");

            VertexSplitterTime first = States.Vertices[States.CurrentListIndex];
            VertexSplitterTime second = States.Vertices[States.CurrentListIndex + 1];
            States.CurrentListIndex++;

            HandleMinisegGeneration(first, second);

            bool isDone = (States.CurrentListIndex + 1 >= States.Vertices.Count);
            States.State = (isDone ? MinisegState.Finished : MinisegState.Working);

            if (isDone && JunctionClassifier.IsDanglingJunction(second.Index))
                log.Warn("BSP miniseg generation found dangling junction, BSP tree likely malformed");
        }
    }
}

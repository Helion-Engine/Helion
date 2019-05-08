using Helion.BSP.Geometry;
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
        private VertexAllocator vertexAllocator;

        public MinisegCreator(VertexAllocator allocator) => vertexAllocator = allocator;

        private void HandleMinisegGeneration(VertexSplitterTime first, VertexSplitterTime second)
        {
            // TODO
        }

        public void load(BspSegment splitter, HashSet<VertexIndex> collinearVertices)
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

        public void execute()
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

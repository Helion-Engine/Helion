using Helion.Bsp.Geometry;
using Helion.Util.Geometry;
using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.States.Miniseg
{
    /// <summary>
    /// The instance responsible for creating minisegs, which are segments that
    /// do not exist in the lines of the original map but are required to make
    /// a convex enclosed polygon (aka, the subsector, a leaf in the BSP tree).
    /// </summary>
    public class MinisegCreator
    {
        /// <summary>
        /// The states for this object.
        /// </summary>
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

        /// <summary>
        /// Loads all the collinear vertices that were found from the splitter.
        /// </summary>
        /// <param name="splitter">The splitter that was used.</param>
        /// <param name="collinearVertices">All the vertices that lay on the
        /// line of the splitter, including ones that were created by the 
        /// splitter when partitioning the map.</param>
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

        /// <summary>
        /// Advances to the next state, which may create a miniseg.
        /// </summary>
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

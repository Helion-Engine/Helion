using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.States.Miniseg
{
    /// <summary>
    /// A debuggable miniseg creator that moves in an atomic stepwise fashion.
    /// </summary>
    public class SteppableMinisegCreator : IMinisegCreator
    {
        /// <summary>
        /// The states for this object.
        /// </summary>
        public MinisegStates States { get; private set; } = new MinisegStates();

        private readonly VertexAllocator m_vertexAllocator;
        private readonly SegmentAllocator m_segmentAllocator;
        private readonly JunctionClassifier m_junctionClassifier;

        /// <summary>
        /// Creates a new debuggable miniseg creator.
        /// </summary>
        /// <param name="vertexAllocator">The vertex allocator.</param>
        /// <param name="segmentAllocator">The segment allocator.</param>
        /// <param name="junctionClassifier">The junction classifier.</param>
        public SteppableMinisegCreator(VertexAllocator vertexAllocator, SegmentAllocator segmentAllocator, 
            JunctionClassifier junctionClassifier)
        {
            m_vertexAllocator = vertexAllocator;
            m_segmentAllocator = segmentAllocator;
            m_junctionClassifier = junctionClassifier;
        }

        /// <inheritdoc/>
        public void Load(BspSegment splitter, HashSet<int> collinearVertices)
        {
            Precondition(collinearVertices.Count >= 2, "Requires two or more vertices for miniseg generation (the splitter should have contributed two)");

            States = new MinisegStates();

            foreach (int vertexIndex in collinearVertices)
            {
                double splitterTime = splitter.ToTime(m_vertexAllocator[vertexIndex]);
                States.Vertices.Add(new VertexSplitterTime(vertexIndex, splitterTime));
            }

            States.Vertices.Sort();
        }

        /// <inheritdoc/>
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

        private void HandleMinisegGeneration(VertexSplitterTime first, VertexSplitterTime second)
        {
            States.VoidStatus = VoidStatus.NotInVoid;

            // If a segment exists for the vertices then we're walking along a
            // segment that was collinear with the splitter, so we don't need a
            // miniseg.
            if (m_segmentAllocator.ContainsSegment(first.Index, second.Index))
                return;

            Vec2D secondVertex = m_vertexAllocator[second.Index];
            if (m_junctionClassifier.CheckCrossingVoid(first.Index, secondVertex))
                States.VoidStatus = VoidStatus.InVoid;
            else
            {
                BspSegment miniseg = m_segmentAllocator.GetOrCreate(first.Index, second.Index);
                States.Minisegs.Add(miniseg);
            }
        }
    }
}
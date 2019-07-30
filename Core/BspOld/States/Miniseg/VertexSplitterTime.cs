using Helion.BspOld.Geometry;
using System;

namespace Helion.BspOld.States.Miniseg
{
    /// <summary>
    /// A simple wrapper around the time that a vertex was created.
    /// </summary>
    public class VertexSplitterTime : IComparable<VertexSplitterTime>
    {
        /// <summary>
        /// The index of the vertex.
        /// </summary>
        public VertexIndex Index;

        /// <summary>
        /// The time along the splitter segment that this vertex was made.
        /// </summary>
        public double tSplitter;

        public VertexSplitterTime(VertexIndex index, double splitterTime)
        {
            Index = index;
            tSplitter = splitterTime;
        }

        public int CompareTo(VertexSplitterTime other) => tSplitter.CompareTo(other.tSplitter);
    }
}

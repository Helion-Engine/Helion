using System;

namespace Helion.Bsp.States.Miniseg
{
    /// <summary>
    /// A simple wrapper around the time that a vertex was created.
    /// </summary>
    public class VertexSplitterTime : IComparable<VertexSplitterTime>
    {
        /// <summary>
        /// The index of the vertex.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// The time along the splitter segment that this vertex was made.
        /// </summary>
        public readonly double SplitterTime;

        /// <summary>
        /// Creates an index/time pair.
        /// </summary>
        /// <param name="index">The index of the vertex.</param>
        /// <param name="splitterTime">The time this is relative to the
        /// splitter.</param>
        public VertexSplitterTime(int index, double splitterTime)
        {
            Index = index;
            SplitterTime = splitterTime;
        }

        /// <inheritdoc/>
        public int CompareTo(VertexSplitterTime other) => SplitterTime.CompareTo(other.SplitterTime);
    }
}
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Geometry
{
    /// <summary>
    /// A table that allows quick lookups based on two vertex indices.
    /// </summary>
    public class SegmentLookupTable
    {
        private readonly Dictionary<int, VertexSegmentPairList> m_table = new Dictionary<int, VertexSegmentPairList>();

        /// <summary>
        /// Gets the segment if it exists in the table.
        /// </summary>
        /// <param name="firstIndex">The first vertex index.</param>
        /// <param name="secondIndex">The second vertex index.</param>
        /// <returns>The segment if one exists, or null if not.</returns>
        public BspSegment? this[int firstIndex, int secondIndex] => TryGetValue(firstIndex, secondIndex, out BspSegment? seg) ? seg : null;

        /// <summary>
        /// Clears the table of all segments.
        /// </summary>
        public void Clear()
        {
            m_table.Clear();
        }
        
        /// <summary>
        /// Adds a new segment to be tracked by this table.
        /// </summary>
        /// <param name="segment">The segment to add. This should not already
        /// be in the table.</param>
        public void Add(BspSegment segment)
        {
            (int minIndex, int maxIndex) = MathHelper.MinMax(segment.StartIndex, segment.EndIndex);

            if (m_table.TryGetValue(minIndex, out VertexSegmentPairList? vertexSegPairs))
            {
                vertexSegPairs.Add(maxIndex, segment);
                return;
            }
            
            VertexSegmentPairList pairList = new VertexSegmentPairList();
            pairList.Add(maxIndex, segment);
            m_table[minIndex] = pairList;
        }

        /// <summary>
        /// Checks if a segment is in this table with the provided indices. The
        /// order of indices does not matter.
        /// </summary>
        /// <param name="firstVertexIndex">The first index.</param>
        /// <param name="secondVertexIndex">The second index.</param>
        /// <returns>True if it exists, false if not.</returns>
        public bool Contains(int firstVertexIndex, int secondVertexIndex)
        {
            (int minIndex, int maxIndex) = MathHelper.MinMax(firstVertexIndex, secondVertexIndex);

            if (m_table.TryGetValue(minIndex, out VertexSegmentPairList? vertexSegPairs))
                return vertexSegPairs.Contains(maxIndex);
            return false;
        }

        /// <summary>
        /// Tries to get the value provided. Order of the indices does not have
        /// any effect on the result.
        /// </summary>
        /// <param name="firstVertexIndex">The first vertex index.</param>
        /// <param name="secondVertexIndex">The second vertex index.</param>
        /// <param name="segment">The segment to be set with the value if it
        /// does exist.</param>
        /// <returns>True if it does exist, false if not (and is unsafe to
        /// use the out value).</returns>
        public bool TryGetValue(int firstVertexIndex, int secondVertexIndex, [MaybeNullWhen(false)] out BspSegment segment)
        {
            (int minIndex, int maxIndex) = MathHelper.MinMax(firstVertexIndex, secondVertexIndex);

            if (m_table.TryGetValue(minIndex, out VertexSegmentPairList? vertexSegPairs))
            {
                if (vertexSegPairs.TryGetSegIndex(maxIndex, out BspSegment? foundSeg))
                {
                    segment = foundSeg;
                    return true;
                }
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            segment = default;
#pragma warning restore CS8625

            return false;
        }

        private class VertexSegmentPairList
        {
            private readonly List<(int vertexIndex, BspSegment segment)> m_pairs = new List<(int, BspSegment)>();

            internal bool Contains(int largerVertexIndex)
            {
                return m_pairs.Any(pair => pair.vertexIndex == largerVertexIndex);
            }

            internal void Add(int maxIndex, BspSegment segment)
            {
                Precondition(!Contains(maxIndex), "Trying to add the same vertex/seg pair twice");

                m_pairs.Add((maxIndex, segment));
            }

            internal bool TryGetSegIndex(int maxIndex, [MaybeNullWhen(false)] out BspSegment segment)
            {
                for (int i = 0; i < m_pairs.Count; i++)
                {
                    (int vertexIndex, BspSegment seg) = m_pairs[i];
                    if (maxIndex == vertexIndex)
                    {
                        segment = seg;
                        return true;
                    }
                }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                segment = default;
#pragma warning restore CS8625

                return false;
            }
        }
    }
}
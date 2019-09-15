using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Util.Extensions;
using MoreLinq;
using MoreLinq.Extensions;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.BspOld.Geometry
{
    /// <summary>
    /// Identifies and handles chains of segments which should not be eligible
    /// for consideration when doing BSP building.
    /// </summary>
    /// <remarks>
    /// <para>The algorithm works as follows. It will take a series of segments
    /// and create a graph out of all the vertices. It will then remove each
    /// terminal chain it finds until none are left. In a completely degenerate
    /// map, this could include every single vertex.</para>
    /// <para>It starts out by indexing all the terminal nodes in a terminal
    /// chain. After that, it will recursively cut away the chain until it
    /// reaches a node that is no longer terminal (ex: a node that has 3
    /// connections, which then becomes 2 after we cut off the chain) or it
    /// reaches the other terminating end of the chain.</para>
    /// <para>This process keeps getting repeated until the set of terminal
    /// nodes are empty. At the end, every segment that is removed is placed
    /// into the <see cref="PrunedSegments"/> set.</para>
    /// </remarks>
    public class SegmentChainPruner
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// A list of all the pruned segments. These may harmless lines or they
        /// could be lines that would cause problems with BSP building.
        /// </summary>
        public readonly HashSet<BspSegment> PrunedSegments = new HashSet<BspSegment>();
        
        private readonly SegmentLookupTable m_segmentTable = new SegmentLookupTable();
        private readonly Dictionary<int, List<int>> m_vertexAdjacencyList = new Dictionary<int, List<int>>();
        private readonly HashSet<int> m_terminalChainTails = new HashSet<int>();
        
        /// <summary>
        /// Goes through all the segments and prunes all the terminal chains.
        /// </summary>
        /// <param name="segments">The segments to prune. This list will be
        /// modified.</param>
        /// <returns>A new list of the non-pruned segments. This may return the
        /// list passed in if it was unchanged, or it may return a completely
        /// new list.</returns>
        public List<BspSegment> Prune(List<BspSegment> segments)
        {
            ClearDataStructures();
            AddSegmentsToAdjacencyList(segments);
            DiscoverTerminalChains();
            RemoveAllTerminalChains();

            if (PrunedSegments.Count > 0)
                Log.Debug("BSP builder pruned {0} dangling segments", PrunedSegments.Count);

            return CalculatePrunedSegments(segments);
        }

        private List<BspSegment> CalculatePrunedSegments(List<BspSegment> segments)
        {
            if (PrunedSegments.Empty())
                return segments;
            return segments.Where(seg => !PrunedSegments.Contains(seg)).ToList();
        }

        private void ClearDataStructures()
        {
            PrunedSegments.Clear();
            m_segmentTable.Clear();
            m_vertexAdjacencyList.Clear(); 
            m_terminalChainTails.Clear();
        }

        private void AddSegmentsToAdjacencyList(List<BspSegment> segments)
        {
            foreach (BspSegment segment in segments)
            {
                m_segmentTable.Add(segment);
                AddToAdjacencyList(segment.StartIndex, segment.EndIndex);
                AddToAdjacencyList(segment.EndIndex, segment.StartIndex);
            }

            void AddToAdjacencyList(int beginIndex, int endIndex)
            {
                if (m_vertexAdjacencyList.TryGetValue(beginIndex, out List<int>? indices))
                    indices.Add(endIndex);
                else
                    m_vertexAdjacencyList[beginIndex] = new List<int> { endIndex };
            }
        }

        private void DiscoverTerminalChains()
        {
            MoreEnumerable.ForEach(m_vertexAdjacencyList.Where(intToListPair => intToListPair.Value.Count == 1), intToListPair => m_terminalChainTails.Add(intToListPair.Key));
        }

        private void RemoveAllTerminalChains()
        {
            // We can't remove while iterating so we have to do this annoying
            // stuff unfortunately.
            HashSet<int> indicesToRemove = ToHashSetExtension.ToHashSet(m_terminalChainTails);

            foreach (int index in indicesToRemove)
            {
                // It is possible we removed it while trimming a chain. This
                // will occur when removing double-ended terminal chain.
                if (!m_terminalChainTails.Contains(index))
                    continue;
                
                (int endingIndex, bool wasDoubleEnded) = RemoveTerminalChain(index);
                m_terminalChainTails.Remove(index);

                if (wasDoubleEnded)
                    m_terminalChainTails.Remove(endingIndex);
            }
        }

        private (int endingIndex, bool wasDoubleEnded) RemoveTerminalChain(int index)
        {
            Precondition(m_vertexAdjacencyList.ContainsKey(index), $"Vertex index {index} was somehow not indexed");
            Precondition(m_vertexAdjacencyList[index].Count == 1, "Trying to remove a non-terminal chain");

            List<int> adjacentIndices = m_vertexAdjacencyList[index];
            Invariant(adjacentIndices[0] != index, $"Terminal chain has a self-reference for index {index}");

            return RemoveTerminalChainIteratively(index, adjacentIndices[0]);
        }

        private (int endingIndex, bool wasDoubleEnded) RemoveTerminalChainIteratively(int currentIndex, int nextIndex)
        {
            while (true)
            {
                PruneSegment(currentIndex, nextIndex);

                if (!m_vertexAdjacencyList.TryGetValue(nextIndex, out List<int>? nextIndexList)) 
                    return (nextIndex, true);

                if (nextIndexList.Count >= 2) 
                    return (nextIndex, false);

                int adjacentNextIndex = (nextIndexList[0] == currentIndex ? nextIndexList[1] : nextIndexList[0]);
                currentIndex = nextIndex;
                nextIndex = adjacentNextIndex;
            }
        }

        private void PruneSegment(int currentIndex, int nextIndex)
        {
            BspSegment? segment = m_segmentTable[currentIndex, nextIndex];
            
            if (segment == null)
                throw new NullReferenceException("Cannot prune a segment that doesn't exist");
            Invariant(!PrunedSegments.Contains(segment), "Trying to prune a segment we already pruned");
            
            PrunedSegments.Add(segment);

            RemoveFromAdjacencyList(currentIndex, nextIndex);
        }

        private void RemoveFromAdjacencyList(int currentIndex, int nextIndex)
        {
            RemoveSegment(currentIndex, nextIndex);
            RemoveSegment(nextIndex, currentIndex);

            void RemoveSegment(int firstIndex, int secondIndex)
            {
                List<int> currentAdjList = m_vertexAdjacencyList[firstIndex];
                if (currentAdjList.Count == 1)
                    m_vertexAdjacencyList.Remove(firstIndex);
                else
                    currentAdjList.Remove(secondIndex);
            }
        }
    }
}
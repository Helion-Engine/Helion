using Helion.Bsp.Builder;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Util;
using Helion.Util.Geometry;
using NLog;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.World.Geometry
{
    public class BspTree
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public List<Segment> Segments = new List<Segment>();
        public Subsector[] Subsectors = new Subsector[0];
        private BspNodeCompact[] nodes = new BspNodeCompact[0];
        private uint nextSubsectorIndex;
        private uint nextNodeIndex;

        private BspNodeCompact Root => nodes[^1];

        private BspTree(BspNode root, Map map)
        {
            Precondition(!root.IsDegenerate, "Cannot make a BSP tree from a degenerate build");
            CreateComponents(root, map);
        }

        public static BspTree? Create(Map map)
        {
            OptimizedBspBuilder builder = new OptimizedBspBuilder(map);
            BspNode root = builder.Build();

            if (root.IsDegenerate)
            {
                log.Error("Cannot create BSP tree for map {0}, it is corrupt", map.Name);
                return null;
            }

            return new BspTree(root, map);
        }

        private void CreateComponents(BspNode root, Map map)
        {
            // Since it's a full binary tree, N nodes implies N + 1 leaves.
            int parentNodeCount = root.CalculateParentNodeCount();
            int subsectorNodeCount = parentNodeCount + 1;
            int segmentCountGuess = subsectorNodeCount * 4;

            Segments = new List<Segment>(segmentCountGuess);
            Subsectors = new Subsector[subsectorNodeCount];
            nodes = new BspNodeCompact[parentNodeCount];

            RecursivelyCreateComponents(root, map);

            Postcondition(nextSubsectorIndex <= ushort.MaxValue, "Subsector index overflow (need a 4-byte BSP tree for this map)");
            Postcondition(nextNodeIndex <= ushort.MaxValue, "Node index overflow (need a 4-byte BSP tree for this map)");
        }

        private BspCreateResult RecursivelyCreateComponents(BspNode? node, Map map)
        {
            if (node == null || node.IsDegenerate)
                throw new HelionException("Should never recurse onto a null/degenerate node when composing a world BSP tree");

            if (node.IsSubsector)
                return CreateSubsector(node, map);
            return CreateNode(node, map);
        }

        private BspCreateResult CreateSubsector(BspNode node, Map map)
        {
            List<Segment> clockwiseSegments = CreateClockwiseSegments(node, map);
            Box2Fixed bbox = Box2Fixed.BoundSegments(clockwiseSegments);
            Sector sector = GetSectorFrom(node, map);
            Subsectors[nextSubsectorIndex] = new Subsector((int)nextSubsectorIndex, sector, clockwiseSegments, bbox);

            return BspCreateResult.Subsector(nextSubsectorIndex++);
        }

        private List<Segment> CreateClockwiseSegments(BspNode node, Map map)
        {
            List<Segment> returnSegments = new List<Segment>();
            
            foreach (SubsectorEdge edge in node.ClockwiseEdges)
            {
                Side? side = GetSideFromEdge(edge, map);
                Segment segment = new Segment(Segments.Count, side, edge.Start.ToFixed(), edge.End.ToFixed());
                returnSegments.Add(segment);
                Segments.Add(segment);
            }

            return returnSegments;
        }

        private Side? GetSideFromEdge(SubsectorEdge edge, Map map)
        {
            if (edge.IsMiniseg)
                return null;

            Line line = map.Lines[edge.LineId];
            return edge.IsFront ? line.Front : line.Back;
        }

        private Sector GetSectorFrom(BspNode node, Map map)
        {
            // Even though the Where() clause guarantees us non-null values,
            // Linq doesn't seem to know that yet or can't guarantee. We will
            // just assume this always passes since we've screwed up really bad
            // if the query is empty or
            int sectorId = (from edge in node.ClockwiseEdges
                            where edge.SectorId != null
                            select edge.SectorId).FirstOrDefault() ?? 0;

            Invariant(sectorId >= 0, "Should never run into a negative placeholder index");
            Invariant(sectorId < map.Sectors.Count, "Sector index out of range, is a different map being used than the one for the BSP tree?");

            return map.Sectors[sectorId];
        }

        private BspCreateResult CreateNode(BspNode node, Map map)
        {
            BspCreateResult left = RecursivelyCreateComponents(node.Left, map);
            BspCreateResult right = RecursivelyCreateComponents(node.Right, map);
            Seg2Fixed splitter = MakeSplitterFrom(node.Splitter);
            Box2Fixed bbox = MakeBoundingBoxFrom(left, right);

            BspNodeCompact compactNode = new BspNodeCompact(left.IndexWithBit, right.IndexWithBit, splitter, bbox);
            nodes[nextNodeIndex] = compactNode;

            return BspCreateResult.Node(nextNodeIndex++);
        }

        private Seg2Fixed MakeSplitterFrom(BspSegment? splitter)
        {
            if (splitter == null)
                throw new HelionException("Should never have a parent node with a null splitter");
            return new Seg2Fixed(splitter.Start.ToFixed(), splitter.End.ToFixed());
        }

        private Box2Fixed MakeBoundingBoxFrom(BspCreateResult left, BspCreateResult right)
        {
            Box2Fixed leftBox = (left.IsSubsector ? Subsectors[left.Index].BoundingBox : nodes[left.Index].BoundingBox);
            Box2Fixed rightBox = (right.IsSubsector ? Subsectors[right.Index].BoundingBox : nodes[right.Index].BoundingBox);
            return Box2Fixed.Combine(leftBox, rightBox);
        }

        public Subsector ToSubsector(Vec2Fixed point)
        {
            BspNodeCompact node = Root;

            while (true)
            {
                if (node.Splitter.OnRight(point))
                {
                    if (node.IsRightSubsector)
                        return Subsectors[node.RightChildAsSubsector];
                    else
                        node = nodes[node.RightChild];
                }
                else
                {
                    if (node.IsLeftSubsector)
                        return Subsectors[node.LeftChildAsSubsector];
                    else
                        node = nodes[node.LeftChild];
                }
            }
        }

        public Sector ToSector(Vec2Fixed point) => ToSubsector(point).Sector;
    }

    /// <summary>
    /// A helper class for propagating recursive information when building.
    /// </summary>
    readonly struct BspCreateResult
    {
        /// <summary>
        /// If this result is for a subsector (if true), or a node (if false).
        /// </summary>
        public readonly bool IsSubsector;

        /// <summary>
        /// The index into either <see cref="BspTree.Segments"/> or
        /// <see cref="BspTree.nodes"/> for the component.
        /// </summary>
        public readonly uint Index;

        /// <summary>
        /// Gets the index with the appropriate bit set if needed. This works 
        /// for either the node or the subsector.
        /// </summary>
        public ushort IndexWithBit => (ushort)(IsSubsector ? (Index | BspNodeCompact.IsSubsectorBit) : Index);

        private BspCreateResult(bool isSubsector, uint index)
        {
            IsSubsector = isSubsector;
            Index = index;
        }

        /// <summary>
        /// Creates a result from a subsector index.
        /// </summary>
        /// <param name="index">The subsector index.</param>
        /// <returns>A result with the subsector index.</returns>
        public static BspCreateResult Subsector(uint index) => new BspCreateResult(true, index);

        /// <summary>
        /// Creates a result from a node index.
        /// </summary>
        /// <param name="index">The node index.</param>
        /// <returns>A result with the node index.</returns>
        public static BspCreateResult Node(uint index) => new BspCreateResult(false, index);
    }
}

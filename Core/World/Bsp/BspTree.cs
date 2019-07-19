using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Bsp.Builder;
using Helion.Bsp.Builder.GLBSP;
using Helion.Bsp.Node;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util;
using Helion.Util.Geometry;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Bsp
{
    /// <summary>
    /// The compiled BSP tree that condenses the builder data into a cache
    /// efficient data structure.
    /// </summary>
    public class BspTree
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// All the segments, which are the edges of the subsector.
        /// </summary>
        public List<SubsectorEdge> Segments = new List<SubsectorEdge>();

        /// <summary>
        /// All the subsectors, the convex leaves at the bottom of the BSP 
        /// tree.
        /// </summary>
        public Subsector[] Subsectors = new Subsector[0];

        /// <summary>
        /// A compact struct for all the nodes, specifically to speed up all
        /// recursive BSP traversal.
        /// </summary>
        public BspNodeCompact[] Nodes = new BspNodeCompact[0];

        /// <summary>
        /// The next available subsector index. This is used only for building 
        /// the <see cref="Subsectors"/> list.
        /// </summary>
        private uint m_nextSubsectorIndex;

        /// <summary>
        /// The next available node index. This is used only for building the 
        /// <see cref="Nodes"/> list.
        /// </summary>
        private uint m_nextNodeIndex;

        /// <summary>
        /// The root node of the tree.
        /// </summary>
        /// <remarks>
        /// This is the end index of the nodes array because the recursive
        /// traversal fills in the array from post-order traversal.
        /// </remarks>
        public BspNodeCompact Root => Nodes[Nodes.Length - 1];

        /// <summary>
        /// The index of the root node. This will always be a BSP node.
        /// </summary>
        public uint RootIndex => (uint)(Nodes.Length - 1);

        /// <summary>
        /// Gets the subsector that maps onto the point provided.
        /// </summary>
        /// <param name="point">The point to get the subsector for.</param>
        /// <returns>The subsector for the provided point.</returns>
        public Subsector ToSubsector(Vec2D point)
        {
            BspNodeCompact node = Root;

            // TODO: Would doing the `side ^ 1` optimization get us any perf?
            while (true)
            {
                if (node.Splitter.OnRight(point))
                {
                    if (node.IsRightSubsector)
                        return Subsectors[node.RightChildAsSubsector];
                    node = Nodes[node.RightChild];
                }
                else
                {
                    if (node.IsLeftSubsector)
                        return Subsectors[node.LeftChildAsSubsector];
                    node = Nodes[node.LeftChild];
                }
            }
        }

        /// <summary>
        /// Gets the sector that maps onto the point provided.
        /// </summary>
        /// <param name="point">The point to get the sector for.</param>
        /// <returns>The sector for the provided point.</returns>
        public Sector ToSector(Vec2D point) => ToSubsector(point).Sector;
        
        /// <summary>
        /// Gets the sector that maps onto the point provided.
        /// </summary>
        /// <param name="point">The point to get the sector for.</param>
        /// <returns>The sector for the provided point.</returns>
        public Sector ToSector(Vec3D point) => ToSubsector(point.To2D()).Sector;
        
        private BspTree(BspNode root, Map map)
        {
            Precondition(!root.IsDegenerate, "Cannot make a BSP tree from a degenerate build");
            CreateComponents(root, map);
        }

        private static BspTree? CreateFromInternalBspBuilder(Map map)
        {
            IBspBuilder builderBase = new OptimizedBspBuilderBase(map);
            BspNode? root = builderBase.Build();

            if (root != null) 
                return new BspTree(root, map);
            
            Log.Error("Cannot create BSP tree for map {0}, it is corrupt", map.Name);
            return null;
        }
        
        /// <summary>
        /// Creates a BSP from the map provided. This can fail if the geometry
        /// for the map is corrupt and we cannot make a BSP tree.
        /// </summary>
        /// <param name="map">The map to build the tree from.</param>
        /// <param name="mapEntryCollection">An optional parameter which may
        /// contain GLBSP nodes that we can use.</param>
        /// <returns>A built BSP tree, or a null value if the geometry for the
        /// map is corrupt beyond repair.</returns>
        public static BspTree? Create(Map map, MapEntryCollection? mapEntryCollection = null)
        {
            // For now, we'll attempt to make GLBSP nodes if the level has
            // them. When we don't need this anymore, we will remove it.
            if (mapEntryCollection != null)
            {
                IBspBuilder bspBuilder = new GLBspBuilder(map, mapEntryCollection);
                BspNode? rootNode = bspBuilder.Build();
                if (rootNode != null)
                    return new BspTree(rootNode, map);

                Log.Warn("Unable to build BSP tree from GLBSP nodes for map '{0}', attempting with internal node builder...", map.Name);
            }

            Log.Error("Internal BSP builder disabled currently, cannot build BSP tree");
            return null;
        }
        
        private static Side? GetSideFromEdge(Helion.Bsp.Node.SubsectorEdge edge, Map map)
        {
            if (edge.IsMiniseg)
                return null;

            Line line = map.Lines[edge.LineId];
            return edge.IsFront ? line.Front : line.Back;
        }

        private void CreateComponents(BspNode root, Map map)
        {
            // Since it's a full binary tree, N nodes implies N + 1 leaves.
            int parentNodeCount = root.CalculateParentNodeCount();
            int subsectorNodeCount = parentNodeCount + 1;
            int segmentCountGuess = subsectorNodeCount * 4;

            Segments = new List<SubsectorEdge>(segmentCountGuess);
            Subsectors = new Subsector[subsectorNodeCount];
            Nodes = new BspNodeCompact[parentNodeCount];

            RecursivelyCreateComponents(root, map);

            Postcondition(m_nextSubsectorIndex <= ushort.MaxValue, "Subsector index overflow (need a 4-byte BSP tree for this map)");
            Postcondition(m_nextNodeIndex <= ushort.MaxValue, "Node index overflow (need a 4-byte BSP tree for this map)");
        }

        private BspCreateResult RecursivelyCreateComponents(BspNode? node, Map map)
        {
            if (node == null || node.IsDegenerate)
                throw new HelionException("Should never recurse onto a null/degenerate node when composing a world BSP tree");

            return node.IsSubsector ? CreateSubsector(node, map) : CreateNode(node, map);
        }

        private BspCreateResult CreateSubsector(BspNode node, Map map)
        {
            List<SubsectorEdge> clockwiseSegments = CreateClockwiseSegments(node, map);

            // Apparently contravariance doesn't work with lists...
            List<Seg2D> clockwiseDoubleSegments = clockwiseSegments.Cast<Seg2D>().ToList();
            Box2D bbox = Box2D.BoundSegments(clockwiseDoubleSegments);
            
            Sector sector = GetSectorFrom(node, map);
            Subsectors[m_nextSubsectorIndex] = new Subsector((int)m_nextSubsectorIndex, sector, clockwiseSegments, bbox);

            return BspCreateResult.Subsector(m_nextSubsectorIndex++);
        }

        private List<SubsectorEdge> CreateClockwiseSegments(BspNode node, Map map)
        {
            List<SubsectorEdge> returnSegments = new List<SubsectorEdge>();
            
            foreach (Helion.Bsp.Node.SubsectorEdge edge in node.ClockwiseEdges)
            {
                Side? side = GetSideFromEdge(edge, map);
                SubsectorEdge subsectorEdge = new SubsectorEdge(Segments.Count, side, edge.Start, edge.End);
                
                returnSegments.Add(subsectorEdge);
                Segments.Add(subsectorEdge);
            }

            return returnSegments;
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
            if (node.Splitter == null)
                throw new NullReferenceException("Malformed BSP node, splitter should never be null");
            
            BspCreateResult left = RecursivelyCreateComponents(node.Left, map);
            BspCreateResult right = RecursivelyCreateComponents(node.Right, map);
            Box2D bbox = MakeBoundingBoxFrom(left, right);

            BspNodeCompact compactNode = new BspNodeCompact(left.IndexWithBit, right.IndexWithBit, node.Splitter, bbox);
            Nodes[m_nextNodeIndex] = compactNode;

            return BspCreateResult.Node(m_nextNodeIndex++);
        }

        private Box2D MakeBoundingBoxFrom(BspCreateResult left, BspCreateResult right)
        {
            Box2D leftBox = (left.IsSubsector ? Subsectors[left.Index].BoundingBox : Nodes[left.Index].BoundingBox);
            Box2D rightBox = (right.IsSubsector ? Subsectors[right.Index].BoundingBox : Nodes[right.Index].BoundingBox);
            return Box2D.Combine(leftBox, rightBox);
        }
    }
}
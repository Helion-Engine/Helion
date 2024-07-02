using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Maps.Bsp;
using Helion.Maps.Bsp.Node;
using Helion.Maps.Components;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Geometry.Builder;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Bsp;

/// <summary>
/// The compiled BSP tree that condenses the builder data into a cache
/// efficient data structure.
/// </summary>
public class CompactBspTree
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// All the segments, which are the edges of the subsector.
    /// </summary>
    public DynamicArray<SubsectorSegment> Segments = new();

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
    public BspNodeCompact Root => Nodes[^1];

    private CompactBspTree(BspNode root, GeometryBuilder builder)
    {
        Precondition(!root.IsDegenerate, "Cannot make a BSP tree from a degenerate build");

        CreateComponents(root, builder);

        if (Subsectors.Length == 1)
            HandleSingleSubsectorTree();
    }

    /// <summary>
    /// Creates a BSP from the map provided. This can fail if the geometry
    /// for the map is corrupt and we cannot make a BSP tree.
    /// </summary>
    /// <param name="map">The map to build the tree from.</param>
    /// <param name="builder">The geometry builder for the map.</param>
    /// <param name="bspBuilder">The BSP builder.</param>
    /// <returns>A built BSP tree, or a null value if the geometry for the
    /// map is corrupt beyond repair.</returns>
    public static CompactBspTree? Create(IMap map, GeometryBuilder builder, IBspBuilder bspBuilder)
    {
        BspNode? root = null;

        // Currently the BSP builder has a fair amount of state, and having
        // it detect errors, roll back, and try to repair a map mid-stream
        // while resetting all of its data structures is a lot of work.
        //
        // Further assertions can occur due to malformed maps. The solution
        // now is to attempt it, and if something goes wrong then try to
        // run it with the map repairer and try again. We don't want to run
        // the map repairer from the start because on bigger maps it uses a
        // fair amount of computation due to how the implementation of some
        // algorithms are.
        try
        {
            root = bspBuilder.Build();
        }
        catch
        {
            // Unfortunately malformed maps trigger assertion exceptions.
            // This means map corruption will be impossible to detect as
            // to whether it's map corruption or if it's our fault. For
            // now, we'll have to visit each on a case by case basis and
            // evaluate each corrupt map to see if it really is our fault
            // or not. Therefore we ignore the exception to leave root as
            // null so it can warn the user.
        }

        if (root == null)
        {
            Log.Error("Cannot create BSP tree for map {0}, map geometry corrupt", map.Name);
            return null;
        }

        return new CompactBspTree(root, builder);
    }

    public unsafe int ToSubsectorIndex(double x, double y)
    {
        uint nodeIndex = (uint)Nodes.Length - 1;
        fixed (BspNodeCompact* startNode = &Nodes[0])
        {
            while (true)
            {
                BspNodeCompact* node = startNode + nodeIndex;
                double dot = (node->SplitDelta.X * (y - node->SplitStart.Y)) - (node->SplitDelta.Y * (x - node->SplitStart.X));
                int next = Convert.ToInt32(dot < 0);
                nodeIndex = node->Children[next];

                if ((nodeIndex & BspNodeCompact.IsSubsectorBit) != 0)
                    return (int)(nodeIndex & BspNodeCompact.SubsectorMask);
            }
        }
    }

    public unsafe Subsector ToSubsector(double x, double y)
    {
        int index = ToSubsectorIndex(x, y);
        return Subsectors[index];
    }

    public unsafe Sector ToSector(in Vec3D point)
    {
        int index = ToSubsectorIndex(point.X, point.Y);
        return Subsectors[index].Sector;
    }

    private static Side? GetSideFromEdge(SubsectorEdge edge, GeometryBuilder builder)
    {
        if (edge.Line == null)
            return null;

        // This should never be wrong because the edge line ID's should be
        // shared with the instantiated lines.
        Line line = builder.Lines[edge.Line.Id];

        Precondition(!(line.Back == null && !edge.IsFront), "Trying to get a back side for a one sided line");
        return edge.IsFront ? line.Front : line.Back;
    }

    private void CreateComponents(BspNode root, GeometryBuilder builder)
    {
        // Since it's a full binary tree, N nodes implies N + 1 leaves.
        int parentNodeCount = root.CalculateParentNodeCount();
        int subsectorNodeCount = parentNodeCount + 1;
        int segmentCountGuess = subsectorNodeCount * 4;

        Segments = new(segmentCountGuess);
        Subsectors = new Subsector[subsectorNodeCount];
        Nodes = new BspNodeCompact[parentNodeCount];

        RecursivelyCreateComponents(root, builder);

        Postcondition(m_nextSubsectorIndex <= ushort.MaxValue, "Subsector index overflow (need a 4-byte BSP tree for this map)");
        Postcondition(m_nextNodeIndex <= ushort.MaxValue, "Node index overflow (need a 4-byte BSP tree for this map)");
    }

    private BspCreateResultCompact RecursivelyCreateComponents(BspNode? node, GeometryBuilder builder)
    {
        if (node == null || node.IsDegenerate)
            throw new HelionException("Should never recurse onto a null/degenerate node when composing a world BSP tree");

        return node.IsSubsector ? CreateSubsector(node, builder) : CreateNode(node, builder);
    }

    private readonly List<Seg2D> m_segs = [];

    private BspCreateResultCompact CreateSubsector(BspNode node, GeometryBuilder builder)
    {
        int index = Segments.Length;
        CreateClockwiseSegments(node, builder);

        m_segs.Clear();
        for (int i = 0; i < node.ClockwiseEdges.Count; i++)
        {
            var edge = node.ClockwiseEdges[i];
            m_segs.Add(new Seg2D(edge.Start, edge.End));
        }

        Box2D bbox = Box2D.Bound(m_segs) ?? Box2D.UnitBox;

        Sector sector = GetSectorFrom(node, builder);
        Subsector subsector = new(node.Id, sector, bbox, index, node.ClockwiseEdges.Count);
        Subsectors[m_nextSubsectorIndex] = subsector;

        return BspCreateResultCompact.Subsector(m_nextSubsectorIndex++);
    }

    private void CreateClockwiseSegments(BspNode node, GeometryBuilder builder)
    {
        foreach (SubsectorEdge edge in node.ClockwiseEdges)
        {
            Side? side = GetSideFromEdge(edge, builder);
            SubsectorSegment subsectorEdge = new(side?.Id, edge.Start, edge.End);
            Segments.Add(subsectorEdge);
        }
    }

    private Sector GetSectorFrom(BspNode node, GeometryBuilder builder)
    {
        foreach (SubsectorEdge edge in node.ClockwiseEdges)
        {
            if (edge.Line == null)
                continue;

            // We have built the BSP tree with this kind of line. If it's
            // not, someone has some something unbelievably wrong.
            ILine line = (ILine)edge.Line;
            int sectorId;

            if (line.OneSided)
                sectorId = line.GetFront().GetSector().Id;
            else
            {
                ISide side = edge.IsFront ? line.GetFront() : line.GetBack() !;
                sectorId = side.GetSector().Id;
            }

            // If this ever is wrong, something has gone terribly wrong
            // with building the geometry.
            return builder.Sectors[sectorId];
        }

        throw new HelionException("BSP building malformed, subsector made up of only minisegs (or is a not a leaf)");
    }

    private BspCreateResultCompact CreateNode(BspNode node, GeometryBuilder builder)
    {
        if (node.Splitter == null)
            throw new NullReferenceException("Malformed BSP node, splitter should never be null");

        BspCreateResultCompact left = RecursivelyCreateComponents(node.Left, builder);
        BspCreateResultCompact right = RecursivelyCreateComponents(node.Right, builder);
        Box2D bbox = MakeBoundingBoxFrom(left, right);

        BspNodeCompact compactNode = new BspNodeCompact(left.IndexWithBit, right.IndexWithBit, node.Splitter.Start.Struct, node.Splitter.End.Struct, bbox);
        Nodes[m_nextNodeIndex] = compactNode;

        return BspCreateResultCompact.Node(m_nextNodeIndex++);
    }

    private Box2D MakeBoundingBoxFrom(BspCreateResultCompact left, BspCreateResultCompact right)
    {
        Box2D leftBox = (left.IsSubsector ? Subsectors[left.Index].BoundingBox : Nodes[left.Index].BoundingBox);
        Box2D rightBox = (right.IsSubsector ? Subsectors[right.Index].BoundingBox : Nodes[right.Index].BoundingBox);
        return leftBox.Combine(rightBox);
    }

    private void HandleSingleSubsectorTree()
    {
        Subsector subsector = Subsectors[0];
        SubsectorSegment edge = Segments[0];
        Box2D box = subsector.BoundingBox;

        // Because we want index 0 with the subsector bit set, this is just
        // the subsector bit.
        const uint subsectorIndex = BspNodeCompact.IsSubsectorBit;

        BspNodeCompact root = new BspNodeCompact(subsectorIndex, subsectorIndex, edge.Start, edge.End, box);
        Nodes = new[] { root };
    }
}

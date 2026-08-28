using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Maps.Bsp;
using Helion.Maps.Bsp.Node;
using Helion.Maps.Components;
using Helion.Util;
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
    public SubsectorSegment[] Segments = [];
    public int[] PartnerSegmentSubsectors = [];

    /// <summary>
    /// All the subsectors, the convex leaves at the bottom of the BSP
    /// tree.
    /// </summary>
    public Subsector[] Subsectors = [];

    /// <summary>
    /// A compact struct for all the nodes, specifically to speed up all
    /// recursive BSP traversal.
    /// </summary>
    public BspNodeCompact[] Nodes = [];

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

    private int m_segCount;

    private CompactBspTree(BspNode root, GeometryBuilder builder, int nodeCount, int subsectorCount, int segmentCount)
    {
        Precondition(!root.IsDegenerate, "Cannot make a BSP tree from a degenerate build");
        CreateComponents(root, builder, nodeCount, subsectorCount, segmentCount);

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

        return new CompactBspTree(root, builder, bspBuilder.GetNodeCount(), bspBuilder.GetSubsectorCount(), bspBuilder.GetSegmentCount());
    }

    public unsafe Subsector ToSubsector(uint nodeIndex, double x, double y)
    {
        while (true)
        {
            ref var node = ref Nodes[nodeIndex];

            bool onRight = OnRightNode(x, y, node);
            int next = *(byte*)&onRight;
            nodeIndex = node.Children[next];

            if ((nodeIndex & BspNodeCompact.IsSubsectorBit) != 0)
                return Subsectors[(int)(nodeIndex & BspNodeCompact.SubsectorMask)];
        }        
    }

    public static unsafe bool OnRightNode(double x, double y, in BspNodeCompact node)
    {
        // These checks are required to match dooms behavior for returning different results when exactly on lines w/o deltas
        if (node.SplitDelta.X == 0)
        {
            if (x <= node.SplitStart.X)
                return node.SplitDelta.Y < 0;
            return node.SplitDelta.Y > 0;
        }

        if (node.SplitDelta.Y == 0)
        {
            if (y <= node.SplitStart.Y)
                return node.SplitDelta.X > 0;
            return node.SplitDelta.X < 0;
        }

        double dot = (node.SplitDelta.X * (y - node.SplitStart.Y)) - (node.SplitDelta.Y * (x - node.SplitStart.X));
        return dot < 0;
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

    private void CreateComponents(BspNode root, GeometryBuilder builder, int nodeCount, int subsectorCount, int segmentsCount)
    {
        Segments = new SubsectorSegment[segmentsCount];
        PartnerSegmentSubsectors = new int[segmentsCount];
        Subsectors = new Subsector[subsectorCount];
        Nodes = new BspNodeCompact[nodeCount];

        RecursivelyCreateComponents(root, builder);
    }

    private BspCreateResultCompact RecursivelyCreateComponents(BspNode? node, GeometryBuilder builder)
    {
        if (node == null || node.IsDegenerate)
            throw new HelionException("Should never recurse onto a null/degenerate node when composing a world BSP tree");

        return node.IsSubsector ? CreateSubsector(node, builder) : CreateNode(node, builder);
    }

    private BspCreateResultCompact CreateSubsector(BspNode node, GeometryBuilder builder)
    {
        int index = m_segCount;
        CreateClockwiseSegments(node, builder);

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        for (int i = 0; i < node.ClockwiseEdges.Count; i++)
        {
            var edge = node.ClockwiseEdges[i];
            if (edge.Start.X < minX)
                minX = edge.Start.X;
            if (edge.Start.X > maxX)
                maxX = edge.Start.X;

            if (edge.End.X < minX)
                minX = edge.End.X;
            if (edge.End.X > maxX)
                maxX = edge.End.X;

            if (edge.Start.Y < minY)
                minY = edge.Start.Y;
            if (edge.Start.Y > maxY)
                maxY = edge.Start.Y;

            if (edge.End.Y < minY)
                minY = edge.End.Y;
            if (edge.End.Y > maxY)
                maxY = edge.End.Y;
        }

        Box2D box = new(new Vec2D(minX, minY), new Vec2D(maxX, maxY));
        Sector sector = GetSectorFrom(node, builder);
        Subsector subsector = new(node.Id, sector, box, index, node.ClockwiseEdges.Count);
        Subsectors[m_nextSubsectorIndex] = subsector;

        return BspCreateResultCompact.Subsector(m_nextSubsectorIndex++);
    }

    private void CreateClockwiseSegments(BspNode node, GeometryBuilder builder)
    {
        int sideId, lineId, frontSectorId, backSectorId;
        foreach (SubsectorEdge edge in node.ClockwiseEdges)
        {
            var side = GetSideFromEdge(edge, builder);
            if (side == null)
            {
                sideId = -1;
                lineId = -1;
                frontSectorId = -1;
                backSectorId = -1;
            }
            else
            {
                sideId = side.Id;
                lineId = builder.Sides[sideId].Line.Id;
                frontSectorId = side.Sector.Id;
                backSectorId = side.PartnerSide == null ? -1 : side.PartnerSide.Sector.Id;
            }

            var subsectorEdge = new SubsectorSegment(sideId, lineId, edge.Start, edge.End, frontSectorId, backSectorId);
            Segments[m_segCount] = subsectorEdge;
            PartnerSegmentSubsectors[m_segCount] = edge.Partner?.SubsectorId ?? -1;

            m_segCount++;
        }
    }

    private static Sector GetSectorFrom(BspNode node, GeometryBuilder builder)
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

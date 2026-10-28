using Helion.Geometry;
using Helion.Geometry.Grids;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Loggers;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Geometry.Subsectors;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using zdbspSharp;

namespace Helion.World;

public partial class WorldBase
{
    // Maps bsp node indicies to a blockmap.
    // If the block is contained by a subsector it will be mapped to that subsector.
    // Otherwise it will be the smallest node containing the block.
    private uint[] m_bspBlockmapNodeIndices = [];
    private Dimension m_bspBlockmapDimensions;
    private Vec2D m_bspBlockmapOrigin;

    private unsafe void CreateBspBlockMap(BlockMap blockmap)
    {
        var stopwatch = Stopwatch.StartNew();
        var bspTree = BspTree;
        var bspBlockMapDimensions = BlockMap.CalculateBlockMapDimensions(blockmap.Bounds, BspBlockDimension);
        m_bspBlockmapDimensions = new(bspBlockMapDimensions.Width, bspBlockMapDimensions.Height);
        m_bspBlockmapOrigin = bspBlockMapDimensions.Bounds.Min;
        m_bspBlockmapNodeIndices = new uint[m_bspBlockmapDimensions.Width * m_bspBlockmapDimensions.Height];

        m_bspBlockmapNodeIndices = BspBlockmapBuilder.Build(BspTree, bspBlockMapDimensions.Width, bspBlockMapDimensions.Height, m_bspBlockmapOrigin.X, m_bspBlockmapOrigin.Y, BspBlockDimension);

        //Parallel.For(0, m_bspBlockmapDimensions.Height, y =>
        //{
        //    for (int x = 0; x < m_bspBlockmapDimensions.Width; x++)
        //    {
        //        var minX = x * BspBlockDimension + m_bspBlockmapOrigin.X;
        //        var minY = y * BspBlockDimension + m_bspBlockmapOrigin.Y;
        //        var maxX = minX + BspBlockDimension;
        //        var maxY = minY + BspBlockDimension;

        //        var bspNodeIndex = (uint)bspTree.Nodes.Length - 1;
        //        var blockNodeIndex = bspNodeIndex;

        //        while (true)
        //        {
        //            ref var node = ref bspTree.Nodes[bspNodeIndex];
        //            var onRightBottomLeft = CompactBspTree.OnRightNode(minX, minY, node);
        //            var onRightTopRight = CompactBspTree.OnRightNode(maxX, maxY, node);

        //            if (onRightBottomLeft != onRightTopRight)
        //                break;

        //            var onRightTopLeft = CompactBspTree.OnRightNode(minX, maxY, node);
        //            var onRightBottomRight = CompactBspTree.OnRightNode(maxX, minY, node);
        //            if (onRightBottomLeft != onRightTopLeft || onRightBottomLeft != onRightBottomRight)
        //                break;

        //            blockNodeIndex = bspNodeIndex;

        //            int next = *(byte*)&onRightBottomLeft;
        //            bspNodeIndex = node.Children[next];

        //            if ((bspNodeIndex & BspNodeCompact.IsSubsectorBit) != 0)
        //            {
        //                var containsSubsector = SubsectorContainsBox(bspTree, minX, minY, maxX, maxY, bspNodeIndex);
        //                if (containsSubsector)
        //                    blockNodeIndex = bspNodeIndex;
        //                break;
        //            }
        //        }

        //        m_bspBlockmapNodeIndices[y * m_bspBlockmapDimensions.Width + x] = blockNodeIndex;
        //    }
        ////});
        //for (int y = 0; y < m_bspBlockmapDimensions.Height; y++)
        //{
        //    for (int x = 0; x < m_bspBlockmapDimensions.Width; x++)
        //    {
        //        var minX = x * BspBlockDimension + m_bspBlockmapOrigin.X;
        //        var minY = y * BspBlockDimension + m_bspBlockmapOrigin.Y;
        //        var maxX = minX + BspBlockDimension;
        //        var maxY = minY + BspBlockDimension;

        //        var bspNodeIndex = (uint)bspTree.Nodes.Length - 1;
        //        var blockNodeIndex = bspNodeIndex;

        //        while (true)
        //        {
        //            ref var node = ref bspTree.Nodes[bspNodeIndex];
        //            //var onRightBottomLeft = CompactBspTree.OnRightNode(minX, minY, node);
        //            //var onRightTopRight = CompactBspTree.OnRightNode(maxX, maxY, node);
        //            //if (onRightBottomLeft != onRightTopRight)
        //            //    break;

        //            //var onRightTopLeft = CompactBspTree.OnRightNode(minX, maxY, node);
        //            //var onRightBottomRight = CompactBspTree.OnRightNode(maxX, minY, node);
        //            //if (onRightBottomLeft != onRightTopLeft || onRightBottomLeft != onRightBottomRight)
        //            //    break;

        //            var side = ClassifyBox(minX, minY, maxX, maxY, node);
        //            if (side < 0)
        //                break;

        //            blockNodeIndex = bspNodeIndex;

        //            //int next = *(byte*)&onRightBottomLeft;
        //            bspNodeIndex = node.Children[side];

        //            if ((bspNodeIndex & BspNodeCompact.IsSubsectorBit) != 0)
        //            {
        //                var containsSubsector = SubsectorContainsBox(bspTree, minX, minY, maxX, maxY, bspNodeIndex);
        //                if (containsSubsector)
        //                    blockNodeIndex = bspNodeIndex;
        //                break;
        //            }
        //        }

        //        m_bspBlockmapNodeIndices[y * m_bspBlockmapDimensions.Width + x] = blockNodeIndex;
        //    }
        //}

        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed.TotalMilliseconds;
        HelionLog.Info(elapsed.ToString());

        LastBspBlockmapDimensions = m_bspBlockmapDimensions;
        LastBspBlockmapNodeIndices = m_bspBlockmapNodeIndices;
        LastBspBlockmapOrigin = m_bspBlockmapOrigin;
    }

    internal static int ClassifyBox(double minX, double minY, double maxX, double maxY, in BspNodeCompact node)
    {
        double dx = node.SplitDelta.X, dy = node.SplitDelta.Y;
        double sx = node.SplitStart.X, sy = node.SplitStart.Y;

        if (dx == 0)
        {
            bool lo = minX <= sx, hi = maxX <= sx;
            if (lo != hi)
                return -1;
            return (lo ? dy < 0 : dy > 0) ? 1 : 0;
        }

        if (dy == 0)
        {
            bool lo = minY <= sy, hi = maxY <= sy;
            if (lo != hi)
                return -1;
            return (lo ? dx > 0 : dx < 0) ? 1 : 0;
        }

        // dot = dx*(y - sy) - dy*(x - sx); "right" when dot < 0.
        double yAtMax = dx > 0 ? maxY : minY;
        double yAtMin = dx > 0 ? minY : maxY;
        double xAtMax = dy > 0 ? minX : maxX;
        double xAtMin = dy > 0 ? maxX : minX;

        double dotMax = (dx * (yAtMax - sy)) - (dy * (xAtMax - sx));
        if (dotMax < 0)
            return 1;

        double dotMin = (dx * (yAtMin - sy)) - (dy * (xAtMin - sx));
        if (dotMin >= 0)
            return 0;

        return -1;
    }

    private static bool SubsectorContainsBox(CompactBspTree bspTree, double minX, double minY, double maxX, double maxY, uint bspNodeIndex)
    {
        var subsector = bspTree.Subsectors[bspNodeIndex & BspNodeCompact.SubsectorMask];
        var isContained = minX >= subsector.BoundingBox.Min.X &&
            minY >= subsector.BoundingBox.Min.Y &&
            maxX <= subsector.BoundingBox.Max.X &&
            maxY <= subsector.BoundingBox.Max.Y;
        if (!isContained)
            return false;

        for (int i = subsector.SegIndex; i < subsector.SegIndex + subsector.SegCount; i++)
        {
            ref var seg = ref bspTree.Segments[i];

            //var test = !(seg.Start.X >= minX && seg.Start.Y >= minY && seg.End.X <= maxX && seg.End.Y <= maxY);
            ////if (seg.Start.X >= minX && seg.Start.Y >= minY && seg.End.X <= maxX && seg.End.Y <= maxY)
            ////    continue;

            //var test2 = SegTouchesBox(ref bspTree.Segments[i], minX, minY, maxX, maxY);

            //if (test != test2)
            //{
            //    int lol = 1;
            //}

            if (SegTouchesBox(ref bspTree.Segments[i], minX, minY, maxX, maxY))
                return false;
        }

        return true;
    }

    private static bool SegTouchesBox(ref SubsectorSegment seg, double minX, double minY, double maxX, double maxY)
    {
        var sx = seg.Start.X;
        var sy = seg.Start.Y;
        var ex = seg.End.X;
        var ey = seg.End.Y;
        if (MathHelper.Max(sx, ex) < minX || MathHelper.Min(sx, ex) > maxX || MathHelper.Max(sy, ey) < minY || MathHelper.Min(sy, ey) > maxY)
            return false;

        var dx = ex - sx;
        var dy = ey - sy;
        var c0 = dx * (minY - sy) - dy * (minX - sx);
        var c1 = dx * (minY - sy) - dy * (maxX - sx);
        var c2 = dx * (maxY - sy) - dy * (minX - sx);
        var c3 = dx * (maxY - sy) - dy * (maxX - sx);

        if (c0 >= 0 && c1 >= 0 && c2 >= 0 && c3 >= 0)
            return false;
        if (c0 <= 0 && c1 <= 0 && c2 <= 0 && c3 <= 0)
            return false;

        return true;
    }

    public Subsector ToSubsector(double xPos, double yPos)
    {
        int x = (int)((xPos - m_bspBlockmapOrigin.X)) >> BspBlockShift;
        int y = (int)((yPos - m_bspBlockmapOrigin.Y)) >> BspBlockShift;
        int blockIndex = y * m_bspBlockmapDimensions.Width + x;
        if (blockIndex < 0 || blockIndex >= m_bspBlockmapNodeIndices.Length)
            return BspTree.ToSubsector((uint)BspTree.Nodes.Length - 1, xPos, yPos);

        var startIndex = m_bspBlockmapNodeIndices[blockIndex];
        if ((startIndex & BspNodeCompact.IsSubsectorBit) != 0)
            return BspTree.Subsectors[startIndex & BspNodeCompact.SubsectorMask];

        return BspTree.ToSubsector(startIndex, xPos, yPos);
    }
}

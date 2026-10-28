using Helion.World.Bsp;
using System;

namespace Helion.World;

internal sealed unsafe class BspBlockmapBuilder
{
    private const double BoxEpsilon = 0.01;

    private readonly CompactBspTree m_tree;
    private readonly uint[] m_indices;
    private readonly uint m_root;
    private readonly int m_width;
    private readonly int m_height;
    private readonly int m_dimension;
    private readonly double m_originX;
    private readonly double m_originY;

    private BspBlockmapBuilder(CompactBspTree tree, uint[] indices, int width, int height, double originX, double originY, int dimension)
    {
        m_tree = tree;
        m_indices = indices;
        m_root = (uint)tree.Nodes.Length - 1;
        m_width = width;
        m_height = height;
        m_dimension = dimension;
        m_originX = originX;
        m_originY = originY;
    }

    public static uint[] Build(CompactBspTree tree, int width, int height, double originX, double originY, int dimension)
    {
        var indices = new uint[width * height];
        var builder = new BspBlockmapBuilder(tree, indices, width, height, originX, originY, dimension);
        builder.Resolve(0, 0, width, height, builder.m_root, builder.m_root);
        return indices;
    }

    private double MinX(int x) => x * m_dimension + m_originX;
    private double MinY(int y) => y * m_dimension + m_originY;
    private double MaxX(int x) => MinX(x) + m_dimension;
    private double MaxY(int y) => MinY(y) + m_dimension;

    private void Resolve(int x0, int y0, int x1, int y1, uint nodeIndex, uint lastWholeNodeIndex)
    {
        if (x0 >= x1 || y0 >= y1)
            return;

        var minX = MinX(x0);
        var minY = MinY(y0);
        var maxX = MaxX(x1 - 1);
        var maxY = MaxY(y1 - 1);

        while ((nodeIndex & BspNodeCompact.IsSubsectorBit) == 0)
        {
            ref var node = ref m_tree.Nodes[nodeIndex];
            var side = ClassifyBoxSide(minX, minY, maxX, maxY, in node);
            if (side < 0)
                break;

            lastWholeNodeIndex = nodeIndex;
            nodeIndex = node.Children[side];
        }

        if ((nodeIndex & BspNodeCompact.IsSubsectorBit) != 0 && SubsectorContainsBox(nodeIndex, minX, minY, maxX, maxY))
        {
            for (int y = y0; y < y1; y++)
                new Span<uint>(m_indices, y * m_width + x0, x1 - x0).Fill(nodeIndex);
            return;
        }

        var w = x1 - x0;
        var h = y1 - y0;
        // This is a single block (sized to BspBlockDimension);
        if (w == 1 && h == 1)
        {
            m_indices[y0 * m_width + x0] = lastWholeNodeIndex;
            return;
        }

        // When a dimension is 1 the second half is empty and returns immediately.
        var mx = w > 1 ? x0 + w / 2 : x1;
        var my = h > 1 ? y0 + h / 2 : y1;

        // Split into quad
        Resolve(x0, y0, mx, my, nodeIndex, lastWholeNodeIndex);
        Resolve(mx, y0, x1, my, nodeIndex, lastWholeNodeIndex);
        Resolve(x0, my, mx, y1, nodeIndex, lastWholeNodeIndex);
        Resolve(mx, my, x1, y1, nodeIndex, lastWholeNodeIndex);
    }

    private static int ClassifyBoxSide(double minX, double minY, double maxX, double maxY, in BspNodeCompact node)
    {
        var dx = node.SplitDelta.X;
        var dy = node.SplitDelta.Y;
        var sx = node.SplitStart.X;
        var sy = node.SplitStart.Y;

        if (dx == 0)
        {
            var min = minX <= sx;
            var max = maxX <= sx;
            if (min != max)
                return -1;
            return (min ? dy < 0 : dy > 0) ? 1 : 0;
        }

        if (dy == 0)
        {
            var min = minY <= sy;
            var max = maxY <= sy;
            if (min != max)
                return -1;
            return (min ? dx > 0 : dx < 0) ? 1 : 0;
        }

        // dot = dx*(y - sy) - dy*(x - sx); "right" when dot < 0.
        var yAtMax = dx > 0 ? maxY : minY;
        var yAtMin = dx > 0 ? minY : maxY;
        var xAtMax = dy > 0 ? minX : maxX;
        var xAtMin = dy > 0 ? maxX : minX;

        var dotMax = (dx * (yAtMax - sy)) - (dy * (xAtMax - sx));
        if (dotMax < 0)
            return 1;

        var dotMin = (dx * (yAtMin - sy)) - (dy * (xAtMin - sx));
        if (dotMin >= 0)
            return 0;

        return -1;
    }

    private bool SubsectorContainsBox(uint nodeIndex, double minX, double minY, double maxX, double maxY)
    {
        var subsector = m_tree.Subsectors[nodeIndex & BspNodeCompact.SubsectorMask];
        var segments = m_tree.Segments;

        var iMinX = minX + BoxEpsilon;
        var iMinY = minY + BoxEpsilon;
        var iMaxX = maxX - BoxEpsilon;
        var iMaxY = maxY - BoxEpsilon;

        int end = subsector.SegIndex + subsector.SegCount;
        for (int i = subsector.SegIndex; i < end; i++)
        {
            ref var seg = ref segments[i];
            if (SegBoxIntersection(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, iMinX, iMinY, iMaxX, iMaxY))
                return false;
        }

        return true;
    }

    private static bool SegBoxIntersection(double sx, double sy, double ex, double ey, double minX, double minY, double maxX, double maxY)
    {
        if ((sx <= minX && ex <= minX) || (sx >= maxX && ex >= maxX) || (sy <= minY && ey <= minY) || (sy >= maxY && ey >= maxY))
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
}

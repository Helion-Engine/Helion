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

namespace Helion.World;

public partial class WorldBase
{
    const int BspBlockDimension = 16;
    const int BspBlockShift = 4;

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
        m_bspBlockmapNodeIndices = BspBlockmapBuilder.Build(BspTree, bspBlockMapDimensions.Width, bspBlockMapDimensions.Height, m_bspBlockmapOrigin.X, m_bspBlockmapOrigin.Y, BspBlockDimension);

        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed.TotalMilliseconds;
        HelionLog.Info(elapsed.ToString());

        LastBspBlockmapDimensions = m_bspBlockmapDimensions;
        LastBspBlockmapNodeIndices = m_bspBlockmapNodeIndices;
        LastBspBlockmapOrigin = m_bspBlockmapOrigin;
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

using Helion.Geometry.Grids;
using Helion.Geometry.Vectors;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Geometry.Subsectors;

namespace Helion.World;

public partial class WorldBase
{
    // Maps bsp node indicies to a blockmap.
    // If the block is contained by a subsector it will be mapped to that subsector.
    // Otherwise it will be the smallest node containing the block.
    private uint[] m_bspBlockmapNodeIndices = [];
    private GridDimensions m_bspBlockmapDimensions;

    private unsafe void CreateBspBlockMap(BlockMap blockmap)
    {
        var bspTree = BspTree;
        m_bspBlockmapDimensions = BlockMap.CalculateBlockMapDimensions(blockmap.Bounds, BspBlockDimension);
        m_bspBlockmapNodeIndices = new uint[m_bspBlockmapDimensions.Width * m_bspBlockmapDimensions.Height];
        var origin = m_bspBlockmapDimensions.Bounds.Min;
        for (int y = 0; y < m_bspBlockmapDimensions.Height; y++)
        {
            for (int x = 0; x < m_bspBlockmapDimensions.Width; x++)
            {
                var minX = x * BspBlockDimension + origin.X;
                var minY = y * BspBlockDimension + origin.Y;
                var maxX = minX + BspBlockDimension;
                var maxY = minY + BspBlockDimension;

                var bspNodeIndex = (uint)bspTree.Nodes.Length - 1;
                var blockNodeIndex = bspNodeIndex;

                while (true)
                {
                    ref var node = ref bspTree.Nodes[bspNodeIndex];
                    var onRightBottomLeft = CompactBspTree.OnRightNode(minX, minY, node);
                    var onRightTopRight = CompactBspTree.OnRightNode(maxX, maxY, node);

                    if (onRightBottomLeft != onRightTopRight)
                        break;

                    var onRightTopLeft = CompactBspTree.OnRightNode(minX, maxY, node);
                    var onRightBottomRight = CompactBspTree.OnRightNode(maxX, minY, node);
                    if (onRightBottomLeft != onRightTopLeft || onRightBottomLeft != onRightBottomRight)
                        break;

                    blockNodeIndex = bspNodeIndex;

                    int next = *(byte*)&onRightBottomLeft;
                    bspNodeIndex = node.Children[next];

                    if ((bspNodeIndex & BspNodeCompact.IsSubsectorBit) != 0)
                    {
                        bool containsSubsector = SubsectorContainsBox(bspTree, minX, minY, maxX, maxY, bspNodeIndex);
                        if (containsSubsector)
                            blockNodeIndex = bspNodeIndex;
                        break;
                    }
                }

                m_bspBlockmapNodeIndices[y * m_bspBlockmapDimensions.Width + x] = blockNodeIndex;
            }
        }

        LastBspBlockmapDimensions = m_bspBlockmapDimensions;
        LastBspBlockmapNodeIndices = m_bspBlockmapNodeIndices;
    }

    private unsafe bool SubsectorContainsBox(CompactBspTree bspTree, double minX, double minY, double maxX, double maxY, uint bspNodeIndex)
    {
        var subsector = bspTree.Subsectors[bspNodeIndex & BspNodeCompact.SubsectorMask];
        var isContained = minX >= subsector.BoundingBox.Min.X &&
            minY >= subsector.BoundingBox.Min.Y &&
            maxX <= subsector.BoundingBox.Max.X &&
            maxY <= subsector.BoundingBox.Max.Y;
        if (!isContained)
            return false;

        var containsSubsector = true;
        for (int i = subsector.SegIndex; i < subsector.SegIndex + subsector.SegCount; i++)
        {
            ref var seg = ref BspTree.Segments[i];
            if (seg.Start.X >= minX && seg.Start.Y >= minY && seg.End.X <= maxX && seg.End.Y <= maxY)
                continue;

            containsSubsector = false;
            break;
        }

        return containsSubsector;
    }

    public Subsector ToSubsector(double xPos, double yPos)
    {
        int x = (int)((xPos - m_bspBlockmapDimensions.Bounds.Min.X) / BspBlockDimension);
        int y = (int)((yPos - m_bspBlockmapDimensions.Bounds.Min.Y) / BspBlockDimension);
        int blockIndex = y * m_bspBlockmapDimensions.Width + x;
        if (blockIndex < 0 || blockIndex >= m_bspBlockmapNodeIndices.Length)
            return BspTree.ToSubsector((uint)BspTree.Nodes.Length - 1, xPos, yPos);

        var startIndex = m_bspBlockmapNodeIndices[blockIndex];
        if ((startIndex & BspNodeCompact.IsSubsectorBit) != 0)
            return BspTree.Subsectors[startIndex & BspNodeCompact.SubsectorMask];

        return BspTree.ToSubsector(startIndex, xPos, yPos);
    }
}

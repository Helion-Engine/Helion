using Helion.Geometry.Grids;
using Helion.Geometry.Vectors;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Geometry.Subsectors;
using System.Runtime.CompilerServices;

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
        m_bspBlockmapDimensions = BlockMap.CalculateBlockMapDimensions(blockmap.Bounds, BspBlockDimension);
        m_bspBlockmapNodeIndices = new uint[m_bspBlockmapDimensions.Width * m_bspBlockmapDimensions.Height];
        var origin = m_bspBlockmapDimensions.Bounds.Min;
        for (int y = 0; y < m_bspBlockmapDimensions.Height; y++)
        {
            for (int x = 0; x < m_bspBlockmapDimensions.Width; x++)
            {
                Vec2D min = new(x * BspBlockDimension + origin.X, y * BspBlockDimension + origin.Y);
                Vec2D max = new(min.X + BspBlockDimension, min.Y + BspBlockDimension);

                uint bspNodeIndex = (uint)BspTree.Nodes.Length - 1;
                uint blockNodeIndex = bspNodeIndex;

                while (true)
                {
                    ref var node = ref BspTree.Nodes[bspNodeIndex];
                    bool onRightMin = CompactBspTree.OnRightNode(min.X, min.Y, node);
                    bool onRightMax = CompactBspTree.OnRightNode(max.X, max.Y, node);

                    if (onRightMin != onRightMax)
                        break;

                    blockNodeIndex = bspNodeIndex;

                    int next = *(byte*)&onRightMin;
                    bspNodeIndex = node.Children[next];

                    if ((bspNodeIndex & BspNodeCompact.IsSubsectorBit) != 0)
                    {
                        bool containsSubsector = SubsectorContainsBox(min, max, bspNodeIndex);
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

    private unsafe bool SubsectorContainsBox(in Vec2D min, in Vec2D max, uint bspNodeIndex)
    {
        var subsector = BspTree.Subsectors[bspNodeIndex & BspNodeCompact.SubsectorMask];
        bool containsSubsector = true;

        if (!subsector.BoundingBox.ContainsInclusive(min) || !subsector.BoundingBox.ContainsInclusive(min))
            return false;

        for (int i = subsector.SegIndex; i < subsector.SegIndex + subsector.SegCount; i++)
        {
            ref var seg = ref BspTree.Segments.Data[i];
            if (seg.Start.X >= min.X && seg.Start.Y >= min.Y && seg.End.X <= max.X && seg.End.Y <= max.Y)
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;

namespace Helion.World.Blockmap;

public enum GridIterationStatus
{
    Continue,
    Stop,
}

public readonly record struct GridDimensions(int Width, int Height, Box2D Bounds);

public struct BlockEntities
{
    public int[] EntityIndices;
    public int EntityIndicesLength;
}

public struct BlockLineIndices
{
    public int BlockLineIndex;
    public int BlockLineCount;
}

public class BlockMap
{
    public int Dimension;
    public int Width;
    public int Height;
    public int TotalBlocks;

    public Box2D Bounds;
    public Vec2D Origin;

    public BlockLine[] BlockLines;
    public int BlockLineCount;

    public BlockEntities[] Entities = [];
    public BlockLineIndices[] Lines = [];

    public Entity?[] HeadRenderEntities = [];
    public LinkableList<Island>[] Sectors = [];
    public LinkableList<Island>[] DynamicSectors = [];
    public DynamicArray<Side>[] DynamicSides = [];

    public BlockMap(IList<Line> lines, int blockDimension)
    {
        BlockLines = new BlockLine[lines.Count];
        Bounds = FindMapBoundingBox(lines) ?? new Box2D(Vec2D.Zero, Vec2D.One);
        Dimension = blockDimension;

        var dimensions = CalculateBlockMapDimensions(Bounds, blockDimension);
        Bounds = dimensions.Bounds;
        Width = dimensions.Width;
        Height = dimensions.Height;

        Origin = Bounds.Min;
        TotalBlocks = Width * Height;

        Entities = new BlockEntities[TotalBlocks];
        Lines = new BlockLineIndices[TotalBlocks];

        for (int i = 0; i < TotalBlocks; i++)
            Entities[i].EntityIndices = new int[8];

        AddLinesToBlocks(lines);
    }

    public BlockMap(Box2D bounds, int blockDimension)
    {
        BlockLines = [];
        Bounds = bounds;
        Dimension = blockDimension;

        var dimensions = CalculateBlockMapDimensions(Bounds, blockDimension);
        Bounds = dimensions.Bounds;
        Width = dimensions.Width;
        Height = dimensions.Height;

        Origin = Bounds.Min;
        TotalBlocks = Width * Height;

        HeadRenderEntities = new Entity[TotalBlocks];
        Sectors = new LinkableList<Island>[TotalBlocks];
        DynamicSectors = new LinkableList<Island>[TotalBlocks];
        DynamicSides = new DynamicArray<Side>[TotalBlocks];
    }

    public static GridDimensions CalculateBlockMapDimensions(Box2D bounds, int blockDimension)
    {
        var blockBounds = ToBounds(bounds, blockDimension);
        var sides = blockBounds.Sides;
        int width = (int)(sides.X / blockDimension);
        int height = (int)(sides.Y / blockDimension);
        return new(width, height, blockBounds);
    }

    private static Box2D ToBounds(Box2D bounds, int dimension)
    {
        // Note that we are subtracting 1 from the bottom left even after
        // clamping it to the left. The reason for this is because any
        // iteration over the grid with very clean and fast code has a
        // stupid corner case that causes a diagonal line going from the
        // bottom right corner of [0, 0] to the top left corner of [0, 0]
        // to end up going outside the grid to [-1, 0]. This obviously will
        // cause an exception.
        //
        // The solution is to pad the left block by making it go to the
        // left by one. This way when the edge case happens, it won't be
        // unsafe anymore. The performance impact is technically a net
        // positive because instead of writing more branches in a taxing
        // loop iteration that we want to keep at high speeds, we toss out
        // the branch now for every invocation by doing the following, at
        // the cost of a very small amount of memory in the grid. This is
        // a great trade-off. If we can get the best of both worlds though
        // one day, we should do that.
        int alignedLeftBlock = (int)Math.Floor(bounds.Min.X / dimension) - 1;
        int alignedBottomBlock = (int)Math.Floor(bounds.Min.Y / dimension) - 1;
        int alignedRightBlock = (int)Math.Ceiling(bounds.Max.X / dimension) + 1;
        int alignedTopBlock = (int)Math.Ceiling(bounds.Max.Y / dimension) + 1;

        Vec2D origin = new(alignedLeftBlock * dimension, alignedBottomBlock * dimension);
        Vec2D topRight = new(alignedRightBlock * dimension, alignedTopBlock * dimension);

        return new Box2D(origin, topRight);
    }

    public void Clear()
    {
        for (int i = 0; i < DynamicSectors.Length; i++)
        {
            // Note: Entities are unlinked using UnlinkFromWorld. Only need to dump the other data.
            var sectors = DynamicSectors[i];
            if (sectors != null)
            {
                var islandNode = DynamicSectors[i].Head;
                while (islandNode != null)
                {
                    var nextNode = islandNode.Next;
                    islandNode.Unlink();
                    WorldStatic.DataCache.FreeLinkableNodeIsland(islandNode);
                    islandNode = nextNode;
                }
            }

            var sides = DynamicSides[i];
            sides?.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetBlockIndex(Vec3D position)
    {
        int x = (int)((position.X - Origin.X) / Dimension);
        int y = (int)((position.Y - Origin.Y) / Dimension);
        int index = y * Width + x;
        if (index < 0 || index >= TotalBlocks)
            return -1;

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetBlockIndex(double xPos, double yPos)
    {
        int x = (int)((xPos - Origin.X) / Dimension);
        int y = (int)((yPos - Origin.Y) / Dimension);
        int index = y * Width + x;
        if (index < 0 || index >= TotalBlocks)
            return -1;

        return index;
    }

    public void Link(Entity entity, bool checkLastBlock)
    {
        Assert.Precondition((entity.BlockRange.StartX == Constants.ClearBlock) || checkLastBlock, "Forgot to unlink entity from blockmap");

        var boxMinX = entity.Position.X - entity.Radius;
        var boxMaxX = entity.Position.X + entity.Radius;
        var boxMinY = entity.Position.Y - entity.Radius;
        var boxMaxY = entity.Position.Y + entity.Radius;
        var blockStartX = (short)Math.Max(0, (int)((boxMinX - Origin.X) / Dimension));
        var blockStartY = (short)Math.Max(0, (int)((boxMinY - Origin.Y) / Dimension));
        var blockEndX = (short)Math.Min((int)((boxMaxX - Origin.X) / Dimension), Width - 1);
        var blockEndY = (short)Math.Min((int)((boxMaxY - Origin.Y) / Dimension), Height - 1);

        // If the block range matches then the entity will link to the same blocks.
        // The block stores entities by id in an array so this saves the array copy to remove the index.
        if (checkLastBlock && entity.BlockRange.StartX == blockStartX && entity.BlockRange.StartY == blockStartY && entity.BlockRange.EndX == blockEndX && entity.BlockRange.EndY == blockEndY)
            return;

        entity.UnlinkBlockMapBlocks();
        entity.BlockRange.StartX = blockStartX;
        entity.BlockRange.StartY = blockStartY;
        entity.BlockRange.EndX = blockEndX;
        entity.BlockRange.EndY =  blockEndY;

        for (var by = blockStartY; by <= blockEndY; by++)
        {
            for (var bx = blockStartX; bx <= blockEndX; bx++)
            {
                ref var block = ref Entities[by * Width + bx];
                if (block.EntityIndicesLength == block.EntityIndices.Length)                
                    Array.Resize(ref block.EntityIndices, block.EntityIndices.Length * 2);

                block.EntityIndices[block.EntityIndicesLength++] = entity.Index;
            }
        }
    }

    public void RenderLink(Entity entity)
    {
        Assert.Precondition(entity.RenderBlock == -1, "Forgot to unlink entity from render blockmap");
        entity.RenderBlock = GetBlockIndex(entity.Position);
        if (entity.RenderBlock == -1)
            return;

        var headEntity = HeadRenderEntities[entity.RenderBlock];
        if (headEntity == null)
        {
            HeadRenderEntities[entity.RenderBlock] = entity;
            return;
        }

        entity.RenderBlockNext = headEntity;
        headEntity.RenderBlockPrevious = entity;
        HeadRenderEntities[entity.RenderBlock] = entity;
    }

    public void RemoveRenderLink(Entity entity)
    {
        var headEntity = HeadRenderEntities[entity.RenderBlock];
        if (entity == headEntity)
        {
            headEntity = entity.RenderBlockNext;
            HeadRenderEntities[entity.RenderBlock] = headEntity;
            if (headEntity != null)
                headEntity.RenderBlockPrevious = null;
            entity.RenderBlockNext = null;
            entity.RenderBlockPrevious = null;
            return;
        }

        if (entity.RenderBlockNext != null)
            entity.RenderBlockNext.RenderBlockPrevious = entity.RenderBlockPrevious;
        if (entity.RenderBlockPrevious != null)
            entity.RenderBlockPrevious.RenderBlockNext = entity.RenderBlockNext;

        entity.RenderBlockNext = null;
        entity.RenderBlockPrevious = null;
    }

    public void LinkDynamic(IWorld world, Sector sector)
    {
        Assert.Precondition(sector.BlockmapNodes.Length == 0, "Forgot to unlink sector from blockmap");

        if (sector.Id >= world.Geometry.IslandGeometry.SectorIslands.Length)
            return;

        var islands = world.Geometry.IslandGeometry.SectorIslands[sector.Id];
        foreach (var sectorIsland in islands)
        {
            if (sectorIsland.IsVooDooCloset || sectorIsland.IsMonsterCloset)
                continue;
            var it = CreateBoxIteration(sectorIsland.Box);
            for (int by = it.BlockStartY; by <= it.BlockEndY; by++)
            {
                for (int bx = it.BlockStartX; bx <= it.BlockEndX; bx++)
                {
                    int index = by * it.Width + bx;
                    var node = world.DataCache.GetLinkableNodeIsland(sectorIsland);

                    DynamicSectors[index] ??= new();
                    DynamicSectors[index].Add(node);
                    
                    sector.BlockmapNodes.Add(node);
                }
            }
        }
    }

    public void Link(IWorld world, Sector sector)
    {
        var islands = world.Geometry.IslandGeometry.SectorIslands[sector.Id];
        foreach (var sectorIsland in islands)
        {
            if (sectorIsland.IsVooDooCloset || sectorIsland.IsMonsterCloset)
                continue;
            var it = CreateBoxIteration(sectorIsland.Box);
            for (int by = it.BlockStartY; by <= it.BlockEndY; by++)
            {
                for (int bx = it.BlockStartX; bx <= it.BlockEndX; bx++)
                {
                    var index = by * it.Width + bx;

                    Sectors[index] ??= new();
                    Sectors[index].Add(new() { Value = sectorIsland });
                }
            }
        }
    }

    public void LinkDynamicSide(Side side)
    {
        if (side.Flags.BlockmapLinked)
            return;

        side.Flags.BlockmapLinked = true;
        
        var it = new BlockmapSegIterator(this, side.Line.Segment);
        while (true)
        {
            var index = it.NextIndex();
            if (index == -1)
                break;

            DynamicSides[index] ??= new();
            DynamicSides[index].Add(side);
        }
    }

    private static Box2D? FindMapBoundingBox(IList<Line> lines)
    {
        var boxes = lines.Select(l => l.Segment.Box);
        return Box2D.Combine(boxes);
    }

    private void AddLinesToBlocks(IList<Line> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var it = new BlockmapSegIterator(this, line.Segment);
            while (true)
            {
                var index = it.NextIndex();
                if (index == -1)
                    break;

                if (BlockLineCount == BlockLines.Length)
                {
                    var newLines = new BlockLine[(int)(BlockLines.Length * 1.5)];
                    Array.Copy(BlockLines, newLines, BlockLineCount);
                    BlockLines = newLines;
                }

                BlockLines[BlockLineCount++] = new BlockLine(index, line.Segment, line, line.Back == null, line.Front.Sector, line.Back?.Sector);
            }
        }

        Array.Sort(BlockLines, 0, BlockLineCount, null);
        SetBlockLineIndices();
    }

    private void SetBlockLineIndices()
    {
        int lastIndex = -1;
        ref var lines = ref Lines[0];

        for (int i = 0; i < BlockLineCount; i++)
        {
            ref var blockLine = ref BlockLines[i];
            if (blockLine.BlockIndex != lastIndex)
            {
                lastIndex = blockLine.BlockIndex;
                lines = ref Lines[blockLine.BlockIndex];
                lines.BlockLineIndex = i;
            }

            lines.BlockLineCount++;
        }
    }

    public BlockmapBoxIteration CreateBoxIteration(in Box2D box)
    {
        int startX = (int)((box.Min.X - Origin.X) / Dimension);
        int startY = (int)((box.Min.Y - Origin.Y) / Dimension);
        int endX = (int)((box.Max.X - Origin.X) / Dimension);
        int endY = (int)((box.Max.Y - Origin.Y) / Dimension);
        return new(Math.Max(0, startX), Math.Max(0, startY), Math.Min(Width - 1, endX), Math.Min(Height - 1, endY), Width);
    }

    public BlockmapBoxIteration CreateBoxIteration(double x, double y, double radius)
    {
        int startX = (int)((x - radius - Origin.X) / Dimension);
        int startY = (int)((y - radius - Origin.Y) / Dimension);
        int endX = (int)((x + radius - Origin.X) / Dimension);
        int endY = (int)((y + radius - Origin.Y) / Dimension);
        return new(Math.Max(0, startX), Math.Max(0, startY), Math.Min(Width - 1, endX), Math.Min(Height - 1, endY), Width);
    }

    public BlockmapBoxIteration CreateBoxIteration(double minX, double minY, double maxX, double maxY)
    {
        int startX = (int)((minX - Origin.X) / Dimension);
        int startY = (int)((minY - Origin.Y) / Dimension);
        int endX = (int)((maxX - Origin.X) / Dimension);
        int endY = (int)((maxY - Origin.Y) / Dimension);
        return new(Math.Max(0, startX), Math.Max(0, startY), Math.Min(Width - 1, endX), Math.Min(Height - 1, endY), Width);
    }

    internal int IndexFromBlockCoordinate(Vec2I coordinate) => coordinate.X + (coordinate.Y * Width);
}

public struct BlockmapBoxIteration(int blockStartX, int blockStartY, int blockEndX, int blockEndY, int width)
{
    public int BlockStartX = blockStartX;
    public int BlockStartY = blockStartY;
    public int BlockEndX = blockEndX;
    public int BlockEndY = blockEndY;
    public int Width = width;
}

public ref struct BlockmapSegIterator
{
    private readonly int m_totalBlocks;
    private readonly int m_numBlocks = 1;
    private readonly int m_verticalStep;
    private readonly int m_horizontalStep;
    private int m_blockIndex;
    private int m_blocksVisited;
    private double m_error;
    private double m_absDeltaX;
    private double m_absDeltaY;

    internal BlockmapSegIterator(BlockMap grid, in Seg2D seg)
    {
        m_totalBlocks = grid.TotalBlocks;

        var blockUnitStartX = (seg.Start.X - grid.Origin.X) / grid.Dimension;
        var blockUnitStartY = (seg.Start.Y - grid.Origin.Y) / grid.Dimension;
        var blockUnitEndX = (seg.End.X - grid.Origin.X) / grid.Dimension;
        var blockUnitEndY = (seg.End.Y - grid.Origin.Y) / grid.Dimension;

        var startingBlockX = (int)blockUnitStartX;
        var startingBlockY = (int)blockUnitStartY;
        m_absDeltaX = Math.Abs(blockUnitEndX - blockUnitStartX);
        m_absDeltaY = Math.Abs(blockUnitEndY - blockUnitStartY);
        m_blockIndex = startingBlockX + (startingBlockY * grid.Width);

        if (MathHelper.IsZero(m_absDeltaX))
        {
            m_error = double.MaxValue;
        }
        else if (blockUnitEndX > blockUnitStartX)
        {
            m_horizontalStep = 1;
            m_numBlocks += (int)Math.Floor(blockUnitEndX) - startingBlockX;
            m_error = (Math.Floor(blockUnitStartX) + 1 - blockUnitStartX) * m_absDeltaY;
        }
        else
        {
            m_horizontalStep = -1;
            m_numBlocks += startingBlockX - (int)Math.Floor(blockUnitEndX);
            m_error = (blockUnitStartX - Math.Floor(blockUnitStartX)) * m_absDeltaY;
        }

        if (MathHelper.IsZero(m_absDeltaY))
        {
            m_error = double.MinValue;
        }
        else if (blockUnitEndY > blockUnitStartY)
        {
            m_verticalStep = grid.Width;
            m_numBlocks += (int)Math.Floor(blockUnitEndY) - startingBlockY;
            m_error -= (Math.Floor(blockUnitStartY) + 1 - blockUnitStartY) * m_absDeltaX;
        }
        else
        {
            m_verticalStep = -grid.Width;
            m_numBlocks += startingBlockY - (int)Math.Floor(blockUnitEndY);
            m_error -= (blockUnitStartY - Math.Floor(blockUnitStartY)) * m_absDeltaX;
        }

        if (m_numBlocks > grid.TotalBlocks)
            m_numBlocks = grid.TotalBlocks;
    }

    public int NextIndex()
    {
        if (m_blocksVisited >= m_numBlocks || m_blockIndex < 0 || m_blockIndex >= m_totalBlocks)
            return -1;

        int currentBlockIndex = m_blockIndex;
        m_blocksVisited++;

        if (m_error > 0)
        {
            m_blockIndex += m_verticalStep;
            m_error -= m_absDeltaX;
        }
        else
        {
            m_blockIndex += m_horizontalStep;
            m_error += m_absDeltaY;
        }

        return currentBlockIndex;
    }
}

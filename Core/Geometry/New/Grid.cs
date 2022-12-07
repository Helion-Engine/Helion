using System;
using System.Collections;
using System.Collections.Generic;

namespace Helion.Geometry.New;

public class UniformGrid
{
    public readonly int Dimension;
    public readonly Box2d Bounds;
    public readonly Vec2i BlockDimension;

    public Vec2d Origin => Bounds.Min;

    public UniformGrid(int dimension, Box2d bounds, int padding = 0)
    {
        Dimension = dimension;
        // TODO
    }

    public Vec2i ToBlockCoordinate(Vec2d pos)
    {
        // TODO
        return default;
    }

    public UniformGridSegmentIterator SegIterator(Seg2d seg) => new(this, seg);
    public UniformGridBoxIterator BoxIterator(Box2d box) => new(this, box);
}

public class UniformGrid<TBlock> : UniformGrid, IEnumerable<TBlock> where TBlock : new()
{
    private readonly TBlock[] m_blocks;

    public ReadOnlySpan<TBlock> Blocks => m_blocks;

    public UniformGrid(int dimension, Box2d bounds, int padding = 0) : 
        base(dimension, bounds, padding)
    {
        int numBlocks = BlockDimension.Area;

        m_blocks = new TBlock[numBlocks];
        for (int i = 0; i < numBlocks; i++)
            m_blocks[i] = new();
    }

    public TBlock this[int idx] => m_blocks[idx];

    public int ToBlockIdx(Vec2i coord)
    {
        return (coord.Y * BlockDimension.Width) + coord.X;
    }

    public TBlock ToBlock(Vec2d pos)
    {
        return m_blocks[ToBlockIdx(ToBlockCoordinate(pos))];
    }

    public IEnumerator<TBlock> GetEnumerator() => ((IEnumerable<TBlock>)m_blocks).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => m_blocks.GetEnumerator();
}

public readonly struct UniformGridSegmentIterator
{
    private readonly UniformGrid m_grid;

    public UniformGridSegmentIterator(UniformGrid grid, Seg2d seg)
    {
        m_grid = grid;
    }

    public bool HasNext()
    {
        // TODO
        return false;
    }

    public Vec2i Next()
    {
        // TODO
        return default;
    }
}

public readonly struct UniformGridBoxIterator
{
    private readonly UniformGrid m_grid;

    public UniformGridBoxIterator(UniformGrid grid, Box2d box)
    {
        m_grid = grid;
    }

    public bool HasNext()
    {
        // TODO
        return false;
    }

    public Vec2i Next()
    {
        // TODO
        return default;
    }
}

public readonly struct UniformGridSegmentIterator<TBlock> where TBlock : new()
{
    private readonly UniformGrid<TBlock> m_grid;
    private readonly UniformGridSegmentIterator m_iterator;

    public UniformGridSegmentIterator(UniformGrid<TBlock> grid, Seg2d seg)
    {
        m_grid = grid;
        m_iterator = grid.SegIterator(seg);
    }

    public bool HasNext() => m_iterator.HasNext();
    public TBlock Next() => m_grid[m_grid.ToBlockIdx(m_iterator.Next())];
}

public readonly struct UniformGridBlockIterator<TBlock> where TBlock : new()
{
    private readonly UniformGrid<TBlock> m_grid;
    private readonly UniformGridBoxIterator m_iterator;

    public UniformGridBlockIterator(UniformGrid<TBlock> grid, Box2d box)
    {
        m_grid = grid;
        m_iterator = grid.BoxIterator(box);
    }

    public bool HasNext() => m_iterator.HasNext();
    public TBlock Next() => m_grid[m_grid.ToBlockIdx(m_iterator.Next())];
}

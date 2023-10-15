using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util;

namespace Helion.Geometry.Grids;

/// <summary>
/// A callback return value for grid iteration.
/// </summary>
public enum GridIterationStatus
{
    Continue,
    Stop,
}

/// <summary>
/// A container for a uniformly distributed grid. This also aligns itself
/// to the grid so certain optimizations can be done (ex: power of two
/// shifting instead of divisions).
/// </summary>
/// <typeparam name="T">The block component for each grid element.
/// </typeparam>
public class UniformGrid<T> where T : new()
{
    public readonly int Dimension;

    /// <summary>
    /// How many blocks wide the grid is.
    /// </summary>
    public readonly int Width;

    /// <summary>
    /// How many blocks tall the grid is.
    /// </summary>
    public readonly int Height;

    /// <summary>
    /// The bounds of the grid.
    /// </summary>
    public readonly Box2D Bounds;

    public readonly T[] Blocks;

    /// <summary>
    /// The origin of the grid.
    /// </summary>
    public Vec2D Origin => Bounds.Min;

    public int TotalBlocks => Width * Height;

    /// <summary>
    /// Creates a uniform grid with the bounds provided. Note that the
    /// bounds provided will be expanded for optimization reasons.
    /// </summary>
    /// <param name="bounds">The desired bounds.</param>
    public UniformGrid(Box2D bounds, int blockDimension)
    {
        Dimension = blockDimension;
        Bounds = ToBounds(bounds);

        Vec2D sides = Bounds.Sides;
        Width = (int)(sides.X / Dimension);
        Height = (int)(sides.Y / Dimension);

        Blocks = new T[TotalBlocks];
        for (int i = 0; i < TotalBlocks; i++)
            Blocks[i] = new T();
    }

    /// <summary>
    /// Gets the block at the index provided. Intended primarily for any
    /// iteration where we don't care about the X or Y coordinates and need
    /// something faster.
    /// </summary>
    /// <param name="index">The index of the block.</param>
    /// <exception cref="System.IndexOutOfRangeException">if the index is
    /// out of range.</exception>
    public T this[int index] => Blocks[index];

    /// <summary>
    /// Gets the block at the coordinates provided.
    /// </summary>
    /// <param name="row">The row, should in [0, Width).</param>
    /// <param name="col">The column, should in [0, Height).</param>
    /// <returns>The block at the location.</returns>
    /// <exception cref="System.IndexOutOfRangeException">if the indices
    /// are out of range.</exception>
    public T this[int row, int col] => Blocks[(row * Width) + col];

    /// <summary>
    /// Performs iteration over the grid for the segment provided. The
    /// function will be invoked on every block the segment intersects.
    /// If you want an early out, the other version of Iterate() should
    /// be used instead.
    /// </summary>
    /// <param name="seg">The segment to iterate with.</param>
    /// <param name="func">The function to call for each block it visits.
    /// </param>
    public void Iterate(Seg2D seg, Action<T> func)
    {
        Iterate(seg, block =>
        {
            func(block);
            return GridIterationStatus.Continue;
        });
    }

    /// <summary>
    /// Performs iteration over the grid for the segment provided. The
    /// function will be invoked on every block the segment intersects.
    /// </summary>
    /// <param name="seg">The segment to iterate with.</param>
    /// <param name="func">The function to call for each block it visits,
    /// and the return value tells the function whether it should exit
    /// (true) or continue (false).</param>
    /// <returns>True if it terminated due to a `Stop` condition from the
    /// function, false otherwise.</returns>
    public bool Iterate(Seg2D seg, Func<T, GridIterationStatus> func)
    {
        // This algorithm requires us to be on the unit interval range for
        // our block coordinates. We also want them to be positive, since
        // it will allow us to do well defined bit mask optimizations.
        // Note that 'blockUnit' means it's on the unit range for a block.
        // For example, if its value is 1.5, that means it's half way in
        // between blocks 1 and 2.
        // What this means is we are quantizing the Dimension onto a unit
        // grid.
        Vec2D blockUnitStart = (seg.Start - Origin) / Dimension;
        Vec2D blockUnitEnd = (seg.End - Origin) / Dimension;
        Vec2D absDelta = (blockUnitEnd - blockUnitStart).Abs();

        // This is the index into the block array. Because its a 1D array,
        // moving upwards in the grid is the same adding the grid width.
        // Note that this is okay and does not require flooring because we
        // are assuming it is inside the grid, so when we subtract the
        // origin, it'll always be positive. Positive values when cast to
        // integers are equivalent to flooring.
        Vec2I startingBlock = blockUnitStart.Int;
        int blockIndex = IndexFromBlockCoordinate(startingBlock);

        // This contains the steps that we will add to the block index. As
        // it is a 1D grid, this means horizontal step will be one of {-1,
        // 0, 1}, but the vertical step will be one of {-Width, 0, Width}.
        // The reason the steps are negative is so that we don't need any
        // extra branching to go left or right, we can just add the value
        // and it'll go in the correct direction based on the sign.
        int horizontalStep = 0;
        int verticalStep = 0;

        // Since we are using a Bresenham-like algorithm, this is the error
        // we accumulate when moving along the grid. It tells us whether we
        // want to step along the X axis or the Y axis next.
        double error;

        // We know that the amount of blocks we visit is the number of
        // times we cross the X and Y axes (which we calculate later),
        // plus one for the block we start off in.
        int numBlocks = 1;

        // Look at the X axis. Are we not moving anywhere because we are a
        // vertical line? Or are we going left? Or right?
        if (MathHelper.IsZero(absDelta.X))
        {
            error = double.MaxValue;
        }
        else if (blockUnitEnd.X > blockUnitStart.X)
        {
            horizontalStep = 1;
            numBlocks += (int)Math.Floor(blockUnitEnd.X) - startingBlock.X;
            error = (Math.Floor(blockUnitStart.X) + 1 - blockUnitStart.X) * absDelta.Y;
        }
        else
        {
            horizontalStep = -1;
            numBlocks += startingBlock.X - (int)Math.Floor(blockUnitEnd.X);
            error = (blockUnitStart.X - Math.Floor(blockUnitStart.X)) * absDelta.Y;
        }

        // Same as the above, but now for the Y axis.
        if (MathHelper.IsZero(absDelta.Y))
        {
            error = double.MinValue;
        }
        else if (blockUnitEnd.Y > blockUnitStart.Y)
        {
            verticalStep = Width;
            numBlocks += (int)Math.Floor(blockUnitEnd.Y) - startingBlock.Y;
            error -= (Math.Floor(blockUnitStart.Y) + 1 - blockUnitStart.Y) * absDelta.X;
        }
        else
        {
            verticalStep = -Width;
            numBlocks += startingBlock.Y - (int)Math.Floor(blockUnitEnd.Y);
            error -= (blockUnitStart.Y - Math.Floor(blockUnitStart.Y)) * absDelta.X;
        }

        if (numBlocks > Blocks.Length)
            numBlocks = Blocks.Length;

        for (int i = 0; i < numBlocks && blockIndex < Blocks.Length && blockIndex > 0; i++)
        {
            T gridElement = Blocks[blockIndex];
            if (func(gridElement) == GridIterationStatus.Stop)
                return true;

            // Unfortunately this algorithm will act weird on corners. If
            // you are coming up on a corner and your error is zero, this
            // will go along the X axis first. If you change the equality,
            // then it will favor the Y axis. Either way... there exists a
            // corner case. This used to cause exceptions if it occurred at
            // the origin (since it would go left instead of going up when
            // going from cell [0, 0] to cell [0, 1] through the corner).
            //
            // Since the code is simple and very fast, the grid will just
            // pad itself on the left-side so in the very rare literal
            // corner case it will not go to block (-1, 0) and avoid
            // throwing an exception by being out of bounds.
            if (error > 0)
            {
                blockIndex += verticalStep;
                error -= absDelta.X;
            }
            else
            {
                blockIndex += horizontalStep;
                error += absDelta.Y;
            }
        }

        return false;
    }

    /// <summary>
    /// Iterates over all the boxes that the provided box spans. This will
    /// exit early if the provided function returns `Stop`.
    /// </summary>
    /// <param name="box">The box to check the blocks for overlap.</param>
    /// <param name="func">The handler for each box that is overlapped.
    /// </param>
    /// <returns>True if the function provided ended early by returning
    /// `Stop`, or false if it iterated over all the blocks and each return
    /// value from the provided function was `Continue`.</returns>
    public bool Iterate(Box2D box, Func<T, GridIterationStatus> func)
    {
        // See the Iterate() function for why we don't need to floor here.
        Vec2I blockUnitStart = ((box.Min - Origin) / Dimension).Int;
        Vec2I blockUnitEnd = ((box.Max - Origin) / Dimension).Ceiling().Int;

        // It's less computationally expensive to do row-major iterations
        // than to calculate each cell with `index = y*w + x`, since that
        // involves a multiplication for each lookup. Instead we remember
        // the leftmost index (hence `baseIndex`) and when we want to go
        // to the next row, we just add the `Width` to the base index and
        // we're on the next row. We avoid O(n) muliplication for this very
        // hot loop.
        int baseIndex = blockUnitStart.Y * Width + blockUnitStart.X;
        if (baseIndex < 0)
            baseIndex += Math.Abs(baseIndex) / Width * Width;
        if (baseIndex < 0)
            baseIndex += Width;

        for (int y = blockUnitStart.Y; y < blockUnitEnd.Y; y++)
        {
            int currentIndex = baseIndex;
            for (int x = blockUnitStart.X; x < blockUnitEnd.X && currentIndex < Blocks.Length; x++)
            {
                if (func(Blocks[currentIndex]) == GridIterationStatus.Stop)
                    return true;
                currentIndex++;
            }

            baseIndex += Width;
        }

        return false;
    }

    public T? GetBlock(Vec3D position)
    {
        int x = (int)((position.X - Origin.X) / Dimension);
        int y = (int)((position.Y - Origin.Y) / Dimension);
        int index = y * Width + x;
        if (index < 0 || index >= Blocks.Length)
            return default(T);

        return Blocks[index];
    }

    private Box2D ToBounds(Box2D bounds)
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
        int alignedLeftBlock = (int)Math.Floor(bounds.Min.X / Dimension) - 1;
        int alignedBottomBlock = (int)Math.Floor(bounds.Min.Y / Dimension) - 1;
        int alignedRightBlock = (int)Math.Ceiling(bounds.Max.X / Dimension) + 1;
        int alignedTopBlock = (int)Math.Ceiling(bounds.Max.Y / Dimension) + 1;

        Vec2D origin = new Vec2D(alignedLeftBlock * Dimension, alignedBottomBlock * Dimension);
        Vec2D topRight = new Vec2D(alignedRightBlock * Dimension, alignedTopBlock * Dimension);

        return new Box2D(origin, topRight);
    }

    public BlockmapBoxIteration CreateBoxIteration(in Box2D box)
    {
        Vec2I start = new((int)((box.Min.X - Origin.X) / Dimension), (int)((box.Min.Y - Origin.Y) / Dimension));
        start.X = Math.Max(0, start.X);
        start.Y = Math.Max(0, start.Y);

        Vec2I end = new((int)((box.Max.X - Origin.X) / Dimension), (int)((box.Max.Y - Origin.Y) / Dimension));
        end.X = Math.Min(Width, end.X);
        end.Y = Math.Min(Height, end.Y);

        return new(start, end, Width);
    }

    internal int IndexFromBlockCoordinate(Vec2I coordinate) => coordinate.X + (coordinate.Y * Width);
    
    public BlockmapSegIterator<T> Iterate(in Seg2D seg) => new(this, seg);
}

public readonly struct BlockmapBoxIteration
{
    public readonly Vec2I BlockStart;
    public readonly Vec2I BlockEnd;
    public readonly int Width;

    public BlockmapBoxIteration(Vec2I blockStart, Vec2I blockEnd, int width)
    {
        BlockStart = blockStart;
        BlockEnd = blockEnd;
        Width = width;
    }
}

public ref struct BlockmapSegIterator<T>  where T : new()
{
    private readonly UniformGrid<T> m_grid;
    private readonly int m_totalBlocks;
    private readonly int m_numBlocks = 1;
    private readonly int m_verticalStep;
    private readonly int m_horizontalStep;
    private int m_blockIndex;
    private int m_blocksVisited;
    private double m_error;
    private Vec2D m_absDelta;

    internal BlockmapSegIterator(UniformGrid<T> grid, in Seg2D seg)
    {
        m_grid = grid;
        m_totalBlocks = grid.TotalBlocks;
        
        Vec2D blockUnitStart = (seg.Start - grid.Origin) / grid.Dimension;
        Vec2D blockUnitEnd = (seg.End - grid.Origin) / grid.Dimension;
        Vec2I startingBlock = blockUnitStart.Int;
        m_absDelta = (blockUnitEnd - blockUnitStart).Abs();
        m_blockIndex = grid.IndexFromBlockCoordinate(startingBlock);

        if (MathHelper.IsZero(m_absDelta.X))
        {
            m_error = double.MaxValue;
        }
        else if (blockUnitEnd.X > blockUnitStart.X)
        {
            m_horizontalStep = 1;
            m_numBlocks += (int)Math.Floor(blockUnitEnd.X) - startingBlock.X;
            m_error = (Math.Floor(blockUnitStart.X) + 1 - blockUnitStart.X) * m_absDelta.Y;
        }
        else
        {
            m_horizontalStep = -1;
            m_numBlocks += startingBlock.X - (int)Math.Floor(blockUnitEnd.X);
            m_error = (blockUnitStart.X - Math.Floor(blockUnitStart.X)) * m_absDelta.Y;
        }

        if (MathHelper.IsZero(m_absDelta.Y))
        {
            m_error = double.MinValue;
        }
        else if (blockUnitEnd.Y > blockUnitStart.Y)
        {
            m_verticalStep = grid.Width;
            m_numBlocks += (int)Math.Floor(blockUnitEnd.Y) - startingBlock.Y;
            m_error -= (Math.Floor(blockUnitStart.Y) + 1 - blockUnitStart.Y) * m_absDelta.X;
        }
        else
        {
            m_verticalStep = -grid.Width;
            m_numBlocks += startingBlock.Y - (int)Math.Floor(blockUnitEnd.Y);
            m_error -= (blockUnitStart.Y - Math.Floor(blockUnitStart.Y)) * m_absDelta.X;
        }

        if (m_numBlocks > grid.Blocks.Length)
            m_numBlocks = grid.Blocks.Length;
    }

    public bool HasNext()
    {
        return m_blocksVisited < m_numBlocks && m_blockIndex >= 0 && m_blockIndex < m_totalBlocks;
    }

    public T Next()
    {
        int currentBlockIndex = m_blockIndex;
        m_blocksVisited++;
        
        if (m_error > 0)
        {
            m_blockIndex += m_verticalStep;
            m_error -= m_absDelta.X;
        }
        else
        {
            m_blockIndex += m_horizontalStep;
            m_error += m_absDelta.Y;
        }

        return m_grid.Blocks[currentBlockIndex];
    }
}

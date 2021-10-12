using System.Collections.Generic;
using Helion.Geometry.Grids;
using Helion.Geometry.Vectors;

namespace Helion.Bsp.Geometry;

/// <summary>
/// Responsible for assigning all segments to a unique index where any line
/// segment that has the same index as another segment implies they are
/// collinear.
/// </summary>
/// <remarks>
/// Intended to make it so we skip looking at lines that are collinear to
/// another when we've already determined the splitting cost, as it will
/// be wasted work to check a collinear line since it'd yield the same
/// split cost. This reduces the work done at the bulky O(n^2) step.
/// </remarks>
public class CollinearTracker
{
    private readonly Dictionary<double, int> m_verticalIndices = new();
    private readonly Dictionary<double, int> m_horizontalIndices = new();
    private readonly QuantizedGrid<int> m_slopeInterceptToIndex;
    private int m_nextAvailableIndex;

    /// <summary>
    /// Gets how many indices have been allocated.
    /// </summary>
    /// <remarks>
    /// Intended to be used when creating bit arrays so it knows the max
    /// values to accomodate.
    /// </remarks>
    public int Count => m_nextAvailableIndex;

    /// <summary>
    /// Creates a tracker which has a resolution of sloped lines by the
    /// epsilon provided.
    /// </summary>
    /// <param name="epsilon">How far apart the slope or y-intercept can be
    /// before they are considered the same.</param>
    public CollinearTracker(double epsilon)
    {
        m_slopeInterceptToIndex = new QuantizedGrid<int>(epsilon);
    }

    private static double CalculatePointIndependentSlope(in Vec2D start, in Vec2D end)
    {
        // We want to always evaluate in one direction. This way if we swap
        // start with end at all, then we will always get the same slope.
        if (start.X < end.X)
            return (end.Y - start.Y) / (end.X - start.X);
        return (start.Y - end.Y) / (start.X - end.X);
    }

    /// <summary>
    /// Either gets an existing index for the values provided, or will
    /// allocate a new one if such a combination does not exist.
    /// </summary>
    /// <param name="start">The starting vertex.</param>
    /// <param name="end">The ending vertex.</param>
    /// <returns>The index for the slope/intercept combo.</returns>
    public int GetOrCreateIndex(Vec2D start, Vec2D end)
    {
        if (start.X == end.X)
            return LookupVerticalIndex(start.X);
        if (start.Y == end.Y)
            return LookupHorizontalIndex(start.Y);
        return LookupSlopeIndex(start, end);
    }

    private int LookupVerticalIndex(double x)
    {
        if (m_verticalIndices.TryGetValue(x, out int index))
            return index;

        int newIndex = m_nextAvailableIndex++;
        m_verticalIndices[x] = newIndex;
        return newIndex;
    }

    private int LookupHorizontalIndex(double y)
    {
        if (m_horizontalIndices.TryGetValue(y, out int index))
            return index;

        int newIndex = m_nextAvailableIndex++;
        m_horizontalIndices[y] = newIndex;
        return newIndex;
    }

    private int LookupSlopeIndex(in Vec2D start, in Vec2D end)
    {
        // These are just y = mx + b. However we do the abs of the slope as
        // we want both directions to map onto the same index.
        double m = CalculatePointIndependentSlope(start, end);
        double yIntercept = start.Y - (m * start.X);

        int index = m_slopeInterceptToIndex.GetExistingOrAdd(m, yIntercept, m_nextAvailableIndex);
        if (index == m_nextAvailableIndex)
            m_nextAvailableIndex++;

        return index;
    }
}

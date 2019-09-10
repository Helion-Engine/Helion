using System;
using System.Collections.Generic;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Geometry
{
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
        private readonly Dictionary<double, int> m_verticalIndices = new Dictionary<double, int>();
        private readonly Dictionary<double, int> m_horizontalIndices = new Dictionary<double, int>();
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

        /// <summary>
        /// Either gets an existing index for the values provided, or will
        /// allocate a new one if such a combination does not exist.
        /// </summary>
        /// <param name="start">The starting vertex.</param>
        /// <param name="end">The ending vertex.</param>
        /// <returns>The index for the slope/intercept combo.</returns>
        public int GetOrCreateIndex(Vec2D start, Vec2D end)
        {
            Vec2D delta = end - start;

            if (delta.X == 0)
                return LookupVerticalIndex(start.X);
            if (delta.Y == 0)
                return LookupHorizontalIndex(start.Y);
            return LookupSlopeIndex(start, delta);
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

        private int LookupSlopeIndex(Vec2D start, Vec2D delta)
        {
            Precondition(delta.X != 0 && delta.Y != 0, "Trying to look up a slope for a vertical/horizontal line");
            
            // These are just y = mx + b. However we do the abs of the slope as
            // we want both directions to map onto the same index.
            double m = delta.Y / delta.X;
            double yIntercept = start.Y - (m * start.X);
            double absSlope = Math.Abs(m);
            
            int index = m_slopeInterceptToIndex.GetExistingOrAdd(absSlope, yIntercept, m_nextAvailableIndex);
            if (index == m_nextAvailableIndex)
                m_nextAvailableIndex++;

            return index;
        }
    }
}
using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Container;

/// <summary>
/// Tracks used indices so that a fresh index can always be retrieved. Also
/// allows returning of tracked indices to the pool to be available again.
/// </summary>
/// <remarks>
/// Designed to operate in O(1) time.
/// </remarks>
public class AvailableIndexTracker
{
    /// <summary>
    /// Contains numbers that have been returned to the available pool.
    /// </summary>
    private readonly SortedSet<int> m_availableIndices = new();

    /// <summary>
    /// The highest available index.
    /// </summary>
    /// <remarks>
    /// Will always be larger than anything in the available index set.
    /// </remarks>
    private int m_nextAvailableIndex;

    /// <summary>
    /// How many indices have been allocated at most. This is a safe number
    /// to which any array made of this length will be able to hold indices
    /// that have been allocated from this without issue.
    /// </summary>
    public int Length => m_nextAvailableIndex;

    /// <summary>
    /// Gets the next available index.
    /// </summary>
    /// <returns>The next available untracked index.</returns>
    public int Next()
    {
        if (m_availableIndices.Count <= 0)
        {
            Precondition(m_nextAvailableIndex != int.MaxValue, "Ran out of available indices");
            return m_nextAvailableIndex++;
        }

        int index = m_availableIndices.Min;
        m_availableIndices.Remove(index);
        return index;
    }

    /// <summary>
    /// Returns a number back to the pool that was originally consumed. It
    /// will become available for being consumed via <see cref="Next"/>.
    /// </summary>
    /// <param name="index">The index to make available again.</param>
    public void MakeAvailable(int index)
    {
        Precondition(index >= 0, "No support for negative available indices");
        Precondition(!m_availableIndices.Contains(index), "Trying to return index that is available");

        if (index < 0)
            return;

        // Note that this function means we can run into the case where
        // there are indices that are available adjacently to the next
        // available index. While we could do some bookkeeping to make it
        // so the next available index points to it, the amount of work to
        // make that feasible is a bunch of extra code. In the end it does
        // not gain us any extra performance because we have to remove it
        // anyways at some point. Therefore we leave the code in the simple
        // state that it is instead of complicating it for no real gain.

        if (index == m_nextAvailableIndex - 1 && m_nextAvailableIndex > 0)
            m_nextAvailableIndex--;
        else
            m_availableIndices.Add(index);
    }

    /// <summary>
    /// Checks if the provided index has been allocated by this index
    /// tracker.
    /// </summary>
    /// <remarks>
    /// This always returns false for negative numbers.
    /// </remarks>
    /// <param name="index">The index to check.</param>
    /// <returns>True if it is being tracked, false if not.</returns>
    public bool IsTracked(int index)
    {
        return index >= 0 && !m_availableIndices.Contains(index) && index < m_nextAvailableIndex;
    }
}

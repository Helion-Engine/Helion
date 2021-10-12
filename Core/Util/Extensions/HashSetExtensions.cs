using System.Collections.Generic;

namespace Helion.Util.Extensions;

/// <summary>
/// A series of hash set helper functions.
/// </summary>
public static class HashSetExtensions
{
    /// <summary>
    /// Checks if the hash set is empty or not.
    /// </summary>
    /// <param name="hashSet">The set to check.</param>
    /// <typeparam name="T">The data type for the set.</typeparam>
    /// <returns>True if it has no elements, false otherwise.</returns>
    public static bool Empty<T>(this HashSet<T> hashSet) => hashSet.Count == 0;
}


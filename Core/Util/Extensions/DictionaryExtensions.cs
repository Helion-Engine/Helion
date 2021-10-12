using System.Collections.Generic;

namespace Helion.Util.Extensions;

/// <summary>
/// A series of helper methods for a dictionary.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Checks if a dictionary is empty or not.
    /// </summary>
    /// <param name="dictionary">The dictionary to check.</param>
    /// <typeparam name="K">The key type.</typeparam>
    /// <typeparam name="V">The value type.</typeparam>
    /// <returns>True if it is, false if not.</returns>
    public static bool Empty<K, V>(this Dictionary<K, V> dictionary) => dictionary.Count == 0;
}

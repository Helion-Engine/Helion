using System.Collections.Generic;

namespace Helion.Util.Extensions
{
    /// <summary>
    /// A series of helper methods for a dictionary.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Gets the value, or returns the default value provided.
        /// </summary>
        /// <param name="dictionary">The dictionary to operate on.</param>
        /// <param name="key">The key to check.</param>
        /// <param name="defaultValue">The value to return if no such mapping
        /// exists.</param>
        /// <typeparam name="K">The key type.</typeparam>
        /// <typeparam name="V">The value type.</typeparam>
        /// <returns>The value for a mapping that exists, or the default value
        /// provided as the argument.</returns>
        public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dictionary, K key, V defaultValue)
        {
            return dictionary.TryGetValue(key, out V value) ? value : defaultValue;
        }
    }
}
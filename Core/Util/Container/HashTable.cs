using System.Collections;
using System.Collections.Generic;

namespace Helion.Util.Container
{
    /// <summary>
    /// An entry in a two-dimensional key hash table.
    /// </summary>
    /// <typeparam name="K1">The first key.</typeparam>
    /// <typeparam name="K2">The second key.</typeparam>
    /// <typeparam name="V">The value.</typeparam>
    public struct HashTableEntry<K1, K2, V>
    {
        /// <summary>
        /// The first lookup key.
        /// </summary>
        public K1 FirstKey { get; }

        /// <summary>
        /// The second lookup key.
        /// </summary>
        public K2 SecondKey { get; }

        /// <summary>
        /// The value that was mapped for both keys.
        /// </summary>
        public V Value { get; }

        public HashTableEntry(K1 firstKey, K2 secondKey, V value)
        {
            FirstKey = firstKey;
            SecondKey = secondKey;
            Value = value;
        }
    }

    /// <summary>
    /// Similar to a dictionary, this is a two dimensional table which maps two
    /// keys onto a value.
    /// </summary>
    /// <typeparam name="K1">The first key.</typeparam>
    /// <typeparam name="K2">The second key.</typeparam>
    /// <typeparam name="V">The value.</typeparam>
    public class HashTable<K1, K2, V> : IEnumerable<HashTableEntry<K1, K2, V>> where V : class
    {
        private readonly Dictionary<K1, Dictionary<K2, V>> table = new Dictionary<K1, Dictionary<K2, V>>();

        /// <summary>
        /// Clears all the data from the table.
        /// </summary>
        public void Clear()
        {
            table.Clear();
        }

        /// <summary>
        /// Adds a key, if it exists then overwrites it.
        /// </summary>
        public void AddOrOverwrite(K1 firstKey, K2 secondKey, V value)
        {
            if (table.TryGetValue(firstKey, out Dictionary<K2, V> map))
                map[secondKey] = value;
            else
                table[firstKey] = new Dictionary<K2, V>() { [secondKey] = value };
        }

        /// <summary>
        /// Removes the mapping if it exists.
        /// </summary>
        public void Remove(K1 firstKey, K2 secondKey)
        {
            if (table.TryGetValue(firstKey, out Dictionary<K2, V> map))
                map.Remove(secondKey);
        }

        /// <summary>
        /// Gets a set of the first keys. This will create a new list and place
        /// all of the keys in that list and return that.
        /// </summary>
        /// <returns>A new list of all the first keys.</returns>
        public List<K1> GetFirstKeys()
        {
            List<K1> keys = new List<K1>();
            foreach (K1 key in table.Keys)
                keys.Add(key);
            return keys;
        }

        /// <summary>
        /// Gets the value in the map.
        /// </summary>
        /// <param name="firstKey">The first key.</param>
        /// <param name="secondKey">The second key.</param>
        /// <returns>The value if it exists for the keys, an empty value
        /// otherwise.</returns>
        public V? Get(K1 firstKey, K2 secondKey)
        {
            if (table.TryGetValue(firstKey, out var map))
                if (map.TryGetValue(secondKey, out V value))
                    return value;
            return null;
        }

        /// <summary>
        /// Tries to get the value and if it exists, sets the out value. If it
        /// fails to find it, then the out parameter is set to its default.
        /// </summary>
        /// <param name="firstKey">The first key.</param>
        /// <param name="secondKey">The second key.</param>
        /// <param name="value">The reference to set if found. It not, it is
        /// set to a default value.</param>
        /// <returns>True if the value was found, false if not.</returns>
        public bool TryGet(K1 firstKey, K2 secondKey, ref V? value)
        {
            if (table.TryGetValue(firstKey, out var map))
                if (map.TryGetValue(secondKey, out value))
                    return true;
            return false;
        }

        public IEnumerator<HashTableEntry<K1, K2, V>> GetEnumerator()
        {
            foreach ((K1 firstKey, var map) in table)
                foreach ((K2 secondKey, V value) in map)
                    yield return new HashTableEntry<K1, K2, V>(firstKey, secondKey, value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

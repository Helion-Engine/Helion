using System.Collections.Generic;
using System.Linq;

namespace Helion.Util.Container;

/// <summary>
/// Similar to a dictionary, this is a two dimensional table which maps two
/// keys onto a value.
/// </summary>
/// <typeparam name="K1">The first key.</typeparam>
/// <typeparam name="K2">The second key.</typeparam>
/// <typeparam name="V">The value.</typeparam>
public class HashTable<K1, K2, V> where V : class
{
    private readonly Dictionary<K1, Dictionary<K2, V>> m_table;
    private readonly IEqualityComparer<K2>? m_comparer;

    public HashTable(IEqualityComparer<K1>? comparer1 = null, IEqualityComparer<K2>? comparer2 = null)
    {
        m_table = new Dictionary<K1, Dictionary<K2, V>>(comparer1);
        m_comparer = comparer2;
    }

    /// <summary>
    /// Gets/sets the value at the key pair provided. If it doesn't exist
    /// when getting, it will return null. If creating a value, it will
    /// properly insert it without errors (which may include overwriting
    /// any existing values).
    /// </summary>
    /// <param name="k1">The first key.</param>
    /// <param name="k2">The second key.</param>
    /// <returns>The value mapped for the keys, or null if it does not
    /// exist.</returns>
    public V? this[K1 k1, K2 k2]
    {
        get => Get(k1, k2);
        set
        {
            if (value != null)
                Insert(k1, k2, value);
        }
    }

    /// <summary>
    /// Clears all the data from the table.
    /// </summary>
    public void Clear()
    {
        m_table.Clear();
    }

    /// <summary>
    /// Adds a key, if it exists then overwrites it.
    /// </summary>
    /// <param name="firstKey">The first key.</param>
    /// <param name="secondKey">The second key.</param>
    /// <param name="value">The value for the key mappings.</param>
    public void Insert(K1 firstKey, K2 secondKey, V value)
    {
        if (m_table.TryGetValue(firstKey, out Dictionary<K2, V>? map))
            map[secondKey] = value;
        else
            m_table[firstKey] = new Dictionary<K2, V>(m_comparer) { [secondKey] = value };
    }

    /// <summary>
    /// Removes the mapping if it exists.
    /// </summary>
    /// <param name="firstKey">The first key.</param>
    /// <param name="secondKey">The second key.</param>
    /// <returns>True if it existed, false if there was no mapping for the
    /// keys provided (no element existed).</returns>
    public bool Remove(K1 firstKey, K2 secondKey)
    {
        return m_table.TryGetValue(firstKey, out Dictionary<K2, V>? map) && map.Remove(secondKey);
    }

    /// <summary>
    /// Gets a set of the first keys. This will create a new list and place
    /// all of the keys in that list and return that.
    /// </summary>
    /// <returns>A new list of all the first keys.</returns>
    public IEnumerable<K1> GetFirstKeys() => m_table.Keys.ToList();

    /// <summary>
    /// Gets the value in the map.
    /// </summary>
    /// <param name="firstKey">The first key.</param>
    /// <param name="secondKey">The second key.</param>
    /// <returns>The value if it exists for the keys, an empty value
    /// otherwise.</returns>
    public V? Get(K1 firstKey, K2 secondKey)
    {
        if (m_table.TryGetValue(firstKey, out var map))
            if (map.TryGetValue(secondKey, out V? value))
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
        if (m_table.TryGetValue(firstKey, out var map))
            if (map.TryGetValue(secondKey, out value))
                return true;

        value = default;
        return false;
    }

    /// <summary>
    /// Returns the count of all values.
    /// </summary>
    public int CountAll()
    {
        int count = 0;
        foreach (var key in m_table.Keys)
            count += m_table[key].Values.Count;
        return count;
    }

    /// <summary>
    /// Returns a list of all values.
    /// </summary>
    public List<V> GetValues()
    {
        return m_table.Values.SelectMany(pair => pair.Values).ToList();
    }

    /// <summary>
    /// Returns a list of all values for the given key.
    /// </summary>
    public List<V> GetValues(K1 key)
    {
        return m_table.TryGetValue(key, out var data) ? data.Values.ToList() : new List<V>();
    }
}


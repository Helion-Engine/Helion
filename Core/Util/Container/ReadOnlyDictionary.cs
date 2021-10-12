using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Util.Container;

public class ReadOnlyDictionary<K, V> : ICovariantReadOnlyDictionary<K, V> where V : class
{
    private readonly IDictionary<K, V> m_dictionary;

    public bool Empty => Count == 0;
    public int Count => m_dictionary.Count;

    public ReadOnlyDictionary(IDictionary<K, V> dictionary)
    {
        m_dictionary = dictionary;
    }

    public V this[K key] => m_dictionary[key];

    public bool ContainsKey(K key) => m_dictionary.ContainsKey(key);

    public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
    {
        if (m_dictionary.TryGetValue(key, out V? val))
        {
            value = val;
            return true;
        }

        value = default!;
        return false;
    }

    public IEnumerator<V> GetEnumerator()
    {
        foreach (V value in m_dictionary.Values)
            yield return value;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

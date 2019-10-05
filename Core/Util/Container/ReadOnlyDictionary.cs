using System.Collections;
using System.Collections.Generic;

namespace Helion.Util.Container
{
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

        public bool TryGetValue(K key, out V? value) => m_dictionary.TryGetValue(key, out value);
        
        public IEnumerator<V> GetEnumerator()
        {
            foreach (var (k, v) in m_dictionary)
                yield return v;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}